namespace Quote.Shared.DTOs;

public record QuoteTemplateDto(
    Guid Id,
    string Name,
    string? Description,
    Guid TradeCategoryId,
    string TradeCategoryName,
    decimal DefaultLabourCost,
    decimal? DefaultMaterialsCost,
    int DefaultDurationHours,
    string? DefaultNotes,
    int UsageCount,
    bool IsActive,
    DateTime CreatedAt,
    List<TemplateMaterialDto> Materials
);

public record TemplateMaterialDto(
    Guid Id,
    string ProductName,
    decimal Quantity,
    string? Unit,
    decimal EstimatedUnitPrice,
    decimal TotalPrice
);

public record CreateTemplateRequest(
    string Name,
    string? Description,
    Guid TradeCategoryId,
    decimal DefaultLabourCost,
    decimal? DefaultMaterialsCost,
    int DefaultDurationHours,
    string? DefaultNotes,
    List<CreateTemplateMaterialRequest>? Materials
);

public record CreateTemplateMaterialRequest(
    string ProductName,
    decimal Quantity,
    string? Unit,
    decimal EstimatedUnitPrice
);

public record UpdateTemplateRequest(
    string Name,
    string? Description,
    decimal DefaultLabourCost,
    decimal? DefaultMaterialsCost,
    int DefaultDurationHours,
    string? DefaultNotes,
    bool IsActive,
    List<CreateTemplateMaterialRequest>? Materials
);

public record TemplateListResponse(
    List<QuoteTemplateDto> Templates,
    int TotalCount
);
