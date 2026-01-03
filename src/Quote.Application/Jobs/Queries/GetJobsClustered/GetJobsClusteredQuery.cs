using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.Jobs.Queries.GetJobsClustered;

public record GetJobsClusteredQuery(
    Guid TradieProfileId,
    double? CenterLatitude = null,
    double? CenterLongitude = null,
    double RadiusKm = 50
) : IRequest<Result<JobMapResponse>>;

public record JobMapResponse(
    List<JobMapMarkerDto> Markers,
    List<JobClusterDto> Clusters
);
