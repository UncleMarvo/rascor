using Rascor.Domain;
using Rascor.Domain.Entities;
using Rascor.Domain.Repositories;
using Rascor.Application.DTOs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
        CreateRamsAcceptanceRequest request,
        CancellationToken ct = default)
    {
        // Validate RAMS document exists
        var document = await _documentRepo.GetByIdAsync(request.RamsDocumentId);
        if (document == null)
        {
            throw new InvalidOperationException(
                $"RAMS document {request.RamsDocumentId} not found");
        }

        // Check if already accepted today
        var existingToday = await _acceptanceRepo.HasSignedTodayAsync(
            request.UserId,
            request.SiteId,
            request.WorkAssignmentId);

        if (existingToday)
        {
            _logger.LogWarning(
                "User {UserId} has already signed RAMS at site {SiteId} today",
                request.UserId,
                request.SiteId);
            throw new InvalidOperationException(
                "RAMS document already signed today");
        }

        // Create acceptance
        var acceptance = new RamsAcceptance
        {
            Id = Guid.NewGuid().ToString(),
            UserId = request.UserId,
            SiteId = request.SiteId,
            WorkAssignmentId = request.WorkAssignmentId,
            RamsDocumentId = request.RamsDocumentId,
            SignatureData = request.SignatureData,
            IpAddress = request.IpAddress,
            DeviceInfo = request.DeviceInfo,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            AcceptedAt = _clock.UtcNow.DateTime,
            ChecklistResponses = request.ChecklistResponses != null 
                ? JsonSerializer.Serialize(request.ChecklistResponses) 
                : null
        };

        await _acceptanceRepo.CreateAsync(acceptance);

        _logger.LogInformation(
            "RAMS acceptance created: User={UserId}, Document={DocumentId}, Site={SiteId}",
            request.UserId,
            request.RamsDocumentId,
            request.SiteId);

        return new RamsAcceptanceDto(
            acceptance.Id,
            acceptance.UserId,
            acceptance.SiteId,
            "", // SiteName - would need to load Site
            acceptance.WorkAssignmentId ?? "",
            acceptance.RamsDocumentId,
            document.Title,
            acceptance.AcceptedAt,
            acceptance.IpAddress,
            acceptance.Latitude,
            acceptance.Longitude
        );
    }
}
