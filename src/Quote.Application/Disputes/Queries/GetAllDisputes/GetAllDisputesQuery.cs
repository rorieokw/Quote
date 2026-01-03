using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.Disputes.Queries.GetAllDisputes;

public record GetAllDisputesQuery : IRequest<Result<DisputeListResponse>>
{
    public string? Status { get; init; }
    public string? Reason { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
