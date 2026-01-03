using MediatR;
using Quote.Application.Common.Models;

namespace Quote.Application.QuoteTemplates.Queries.GetTemplates;

public record GetTemplatesQuery : IRequest<Result<GetTemplatesResponse>>
{
    public Guid? TradeCategoryId { get; init; }
    public bool IncludeInactive { get; init; } = false;
}

public record GetTemplatesResponse
{
    public List<TemplateDto> Templates { get; init; } = new();
    public int TotalCount { get; init; }
}

public record TemplateDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid TradeCategoryId { get; init; }
    public string TradeCategoryName { get; init; } = string.Empty;
    public decimal DefaultLabourCost { get; init; }
    public decimal? DefaultMaterialsCost { get; init; }
    public int DefaultDurationHours { get; init; }
    public string? DefaultNotes { get; init; }
    public int UsageCount { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<TemplateMaterialDto> Materials { get; init; } = new();
}

public record TemplateMaterialDto
{
    public Guid Id { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string? Unit { get; init; }
    public decimal EstimatedUnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
}
