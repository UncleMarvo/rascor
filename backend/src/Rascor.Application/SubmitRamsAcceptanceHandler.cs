using Rascor.Domain;
using Rascor.Domain.Entities;
using Rascor.Domain.Repositories;
using Rascor.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Rascor.Application;

public class SubmitRamsAcceptanceHandler
{
    private readonly IRamsAcceptanceRepository _acceptanceRepo;
    private readonly IRamsDocumentRepository _documentRepo;
    private readonly IClock _clock;
    private readonly ILogger<SubmitRamsAcceptanceHandler> _logger;

    public SubmitRamsAcceptanceHandler(
        IRamsAcceptanceRepository acceptanceRepo,
        IRamsDocumentRepository documentRepo,
        IClock clock,
        ILogger<SubmitRamsAcceptanceHandler> logger)
    {
        _acceptanceRepo = acceptanceRepo;
        _documentRepo = documentRepo;
        _clock = clock;
        _logger = logger;
    }

    public async Task<RamsAcceptanceDto> HandleAsync(
        RamsAcceptanceRequest request,
        CancellationToken ct = default)
    {
        // Validate RAMS document exists
        var document = await _documentRepo.GetByIdAsync(request.RamsDocumentId, ct);
        if (document == null)
        {
            throw new InvalidOperationException(
                $"RAMS document {request.RamsDocumentId} not found");
        }

        // Check if already accepted today
        var existingToday = await _acceptanceRepo.HasAcceptedTodayAsync(
            request.UserId,
            request.RamsDocumentId,
            ct);

        if (existingToday)
        {
            _logger.LogWarning(
                "User {UserId} has already accepted RAMS {DocumentId} today",
                request.UserId,
                request.RamsDocumentId);
            throw new InvalidOperationException(
                "RAMS document already accepted today");
        }

        // Create acceptance
        var acceptance = new RamsAcceptance(
            Guid.NewGuid().ToString(),
            request.UserId,
            request.SiteId,
            request.WorkAssignmentId,
            request.RamsDocumentId,
            request.SignatureData,
            request.IpAddress,
            request.DeviceInfo,
            request.Latitude,
            request.Longitude,
            _clock.UtcNow,
            request.ChecklistResponses
        );

        await _acceptanceRepo.AddAsync(acceptance, ct);

        _logger.LogInformation(
            "RAMS acceptance created: User={UserId}, Document={DocumentId}, Site={SiteId}",
            request.UserId,
            request.RamsDocumentId,
            request.SiteId);

        return new RamsAcceptanceDto(
            acceptance.Id,
            acceptance.UserId,
            acceptance.SiteId,
            acceptance.WorkAssignmentId,
            acceptance.RamsDocumentId,
            acceptance.AcceptedAt,
            acceptance.Latitude,
            acceptance.Longitude
        );
    }
}

public record RamsAcceptanceRequest(
    string UserId,
    string SiteId,
    string? WorkAssignmentId,
    string RamsDocumentId,
    string SignatureData,
    string? IpAddress,
    string? DeviceInfo,
    double? Latitude,
    double? Longitude,
    string? ChecklistResponses
);
