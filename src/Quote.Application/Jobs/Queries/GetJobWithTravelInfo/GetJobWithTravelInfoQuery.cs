using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.Jobs.Queries.GetJobWithTravelInfo;

public record GetJobWithTravelInfoQuery(
    Guid JobId,
    double TradieLatitude,
    double TradieLongitude
) : IRequest<Result<JobWithTravelDto>>;
