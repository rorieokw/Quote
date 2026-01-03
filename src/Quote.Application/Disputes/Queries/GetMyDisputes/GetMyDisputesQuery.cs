using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.Disputes.Queries.GetMyDisputes;

public record GetMyDisputesQuery : IRequest<Result<DisputeListResponse>>
{
    public string? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
