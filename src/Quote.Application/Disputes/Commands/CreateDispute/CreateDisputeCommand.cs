using MediatR;
using Quote.Application.Common.Models;

namespace Quote.Application.Disputes.Commands.CreateDispute;

public record CreateDisputeCommand : IRequest<Result<CreateDisputeResponse>>
{
    public Guid JobQuoteId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public record CreateDisputeResponse
{
    public Guid DisputeId { get; init; }
}
