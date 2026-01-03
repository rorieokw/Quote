using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.Quotes.Commands.CreateQuickQuote;

public record CreateQuickQuoteCommand : IRequest<Result<CreateQuickQuoteResponse>>
{
    public Guid JobId { get; init; }
    public decimal LabourCost { get; init; }
    public decimal? MaterialsCost { get; init; }
    public int EstimatedDurationHours { get; init; }
    public string? Notes { get; init; }
    public Guid? TemplateId { get; init; }
    public DateTime? ProposedStartDate { get; init; }
    public bool DepositRequired { get; init; }
    public decimal? DepositPercentage { get; init; }

    // Material items support
    public List<QuoteMaterialRequest>? Materials { get; init; }
    public Guid? MaterialBundleId { get; init; }
    public bool SaveMaterialsAsBundle { get; init; }
    public string? NewBundleName { get; init; }
}

public record CreateQuickQuoteResponse
{
    public Guid QuoteId { get; init; }
    public Guid JobId { get; init; }
    public decimal TotalCost { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime ValidUntil { get; init; }
}
