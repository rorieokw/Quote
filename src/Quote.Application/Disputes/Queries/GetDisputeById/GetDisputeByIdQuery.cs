using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.Disputes.Queries.GetDisputeById;

public record GetDisputeByIdQuery : IRequest<Result<DisputeDto>>
{
    public Guid DisputeId { get; init; }
}
