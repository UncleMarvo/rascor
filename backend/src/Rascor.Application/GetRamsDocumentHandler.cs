using Rascor.Domain.Repositories;
using Rascor.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Rascor.Application;

public class GetRamsDocumentHandler
{
    private readonly IRamsDocumentRepository _repo;
    private readonly ILogger<GetRamsDocumentHandler> _logger;

    public GetRamsDocumentHandler(
        IRamsDocumentRepository repo,
        ILogger<GetRamsDocumentHandler> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<RamsDocumentDto?> ExecuteAsync(
        string documentId, 
        CancellationToken ct = default)
    {
        var document = await _repo.GetByIdAsync(documentId);
        
        if (document == null)
        {
            _logger.LogWarning("RAMS document {DocumentId} not found", documentId);
            return null;
        }
        
        _logger.LogInformation(
            "Retrieved RAMS document {DocumentId} version {Version}", 
            documentId, 
            document.Version);
        
        return new RamsDocumentDto(
            document.Id,
            document.WorkTypeId,
            document.WorkType?.Name ?? "",
            document.Version,
            document.Title,
            document.ContentType,
            document.Content,
            document.PdfBlobUrl,
            document.IsActive,
            document.EffectiveFrom,
            document.ChecklistItems?.Select(item => new RamsChecklistItemDto(
                item.Id,
                item.Section,
                item.DisplayOrder,
                item.ItemType,
                item.Label,
                item.IsRequired,
                item.ValidationRules
            )).ToList()
        );
    }

    public async Task<RamsDocumentDto?> GetCurrentVersionAsync(
        string workTypeId,
        CancellationToken ct = default)
    {
        var document = await _repo.GetCurrentVersionAsync(workTypeId);
        
        if (document == null)
        {
            _logger.LogWarning(
                "No current RAMS document found for work type {WorkTypeId}", 
                workTypeId);
            return null;
        }
        
        return await ExecuteAsync(document.Id, ct);
    }
}
