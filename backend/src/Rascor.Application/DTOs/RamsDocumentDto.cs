namespace Rascor.Application.DTOs;

public record RamsDocumentDto(
    string Id,
    string WorkTypeId,
    string WorkTypeName,
    int Version,
    string Title,
    string ContentType,
    string? Content,
    string? PdfBlobUrl,
    bool IsActive,
    DateTime EffectiveFrom,
    List<RamsChecklistItemDto>? ChecklistItems
);

public record RamsChecklistItemDto(
    string Id,
    string? Section,
    int DisplayOrder,
    string ItemType,
    string Label,
    bool IsRequired,
    string? ValidationRules
);

public record CreateRamsDocumentRequest(
    string WorkTypeId,
    string Title,
    string ContentType,
    string? Content,
    string? PdfBlobUrl,
    List<CreateRamsChecklistItemRequest>? ChecklistItems
);

public record CreateRamsChecklistItemRequest(
    string? Section,
    int DisplayOrder,
    string ItemType,
    string Label,
    bool IsRequired,
    string? ValidationRules
);