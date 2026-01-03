using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.Jobs.Queries.GetJobWithTravelInfo;

public class GetJobWithTravelInfoQueryHandler : IRequestHandler<GetJobWithTravelInfoQuery, Result<JobWithTravelDto>>
{
    private readonly IApplicationDbContext _context;

    public GetJobWithTravelInfoQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<JobWithTravelDto>> Handle(GetJobWithTravelInfoQuery request, CancellationToken cancellationToken)
    {
        var job = await _context.Jobs
            .Include(j => j.TradeCategory)
            .Include(j => j.Customer)
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (job == null)
        {
            return Result<JobWithTravelDto>.Failure("Job not found");
        }

        // Calculate distance using Haversine formula
        double distanceKm = CalculateHaversineDistance(
            request.TradieLatitude,
            request.TradieLongitude,
            job.Latitude,
            job.Longitude
        );

        // Estimate travel time: ~40km/h average in urban areas
        int estimatedMinutes = (int)Math.Ceiling(distanceKm / 40.0 * 60);

        var dto = new JobWithTravelDto(
            Id: job.Id,
            Title: job.Title,
            Description: job.Description,
            SuburbName: job.SuburbName,
            State: job.State.ToString(),
            Postcode: job.Postcode,
            TradeCategoryName: job.TradeCategory?.Name ?? "Unknown",
            BudgetMin: job.BudgetMin,
            BudgetMax: job.BudgetMax,
            IsUrgent: job.PreferredStartDate.HasValue && job.PreferredStartDate.Value <= DateTime.UtcNow.AddDays(2),
            CustomerName: $"{job.Customer?.FirstName} {job.Customer?.LastName}".Trim(),
            DistanceKm: Math.Round(distanceKm, 1),
            EstimatedTravelMinutes: estimatedMinutes,
            JobLatitude: job.Latitude,
            JobLongitude: job.Longitude
        );

        return Result<JobWithTravelDto>.Success(dto);
    }

    private static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth's radius in km

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
