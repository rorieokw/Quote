using MediatR;
using Quote.Application.Common.Models;

namespace Quote.Application.Disputes.Commands.ResolveDispute;

public record ResolveDisputeCommand : IRequest<Result>
{
    public Guid DisputeId { get; init; }
    public string ResolutionType { get; init; } = string.Empty;
    public string Resolution { get; init; } = string.Empty;
    public decimal? RefundAmount { get; init; }
    public string? AdminNotes { get; init; }
}
