namespace Quote.Shared.DTOs;

public record MaterialEstimateTemplateDto(
    Guid Id,
    string Name,
    string? Description,
    Guid TradeCategoryId,
    string TradeCategoryName,
    bool IsSystemTemplate,
    List<MaterialInputDto> Inputs,
    List<MaterialFormulaDto> Materials
);

public record MaterialInputDto(
    string Name,
    string Label,
    string Type,  // number, text, select
    decimal? DefaultValue,
    List<string>? Options  // for select type
);

public record MaterialFormulaDto(
    string Name,
    string Formula,  // e.g., "powerpoints * 2" or "cableRunMeters * 1.1"
    decimal UnitPrice,
    string? Unit
);

public record CalculateMaterialsRequest(
    Guid TemplateId,
    Dictionary<string, decimal> InputValues
);

public record MaterialEstimateResultDto(
    Guid TemplateId,
    string TemplateName,
    List<MaterialLineItemDto> LineItems,
    decimal TotalMaterialsCost
);

public record MaterialLineItemDto(
    string MaterialName,
    decimal Quantity,
    string? Unit,
    decimal UnitPrice,
    decimal TotalPrice
);

public record CreateMaterialTemplateRequest(
    string Name,
    string? Description,
    Guid TradeCategoryId,
    string CalculationFormulaJson
);
