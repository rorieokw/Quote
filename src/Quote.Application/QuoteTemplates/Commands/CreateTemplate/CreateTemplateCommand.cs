using MediatR;
using Quote.Application.Common.Models;

namespace Quote.Application.QuoteTemplates.Commands.CreateTemplate;

public record CreateTemplateCommand : IRequest<Result<CreateTemplateResponse>>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid TradeCategoryId { get; init; }
    public decimal DefaultLabourCost { get; init; }
    public decimal? DefaultMaterialsCost { get; init; }
    public int DefaultDurationHours { get; init; }
    public string? DefaultNotes { get; init; }
    public List<CreateTemplateMaterialCommand>? Materials { get; init; }
}

public record CreateTemplateMaterialCommand
{
    public string ProductName { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public string? Unit { get; init; }
    public decimal EstimatedUnitPrice { get; init; }
}

public record CreateTemplateResponse
{
    public Guid TemplateId { get; init; }
    public string Name { get; init; } = string.Empty;
}
