using MediatR;
using Quote.Application.Common.Models;

namespace Quote.Application.Disputes.Commands.CloseDispute;

public record CloseDisputeCommand : IRequest<Result>
{
    public Guid DisputeId { get; init; }
    public string? Reason { get; init; }
}
