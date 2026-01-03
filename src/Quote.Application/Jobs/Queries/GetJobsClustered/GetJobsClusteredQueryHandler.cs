using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Enums;
using Quote.Shared.DTOs;

namespace Quote.Application.Jobs.Queries.GetJobsClustered;

public class GetJobsClusteredQueryHandler : IRequestHandler<GetJobsClusteredQuery, Result<JobMapResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetJobsClusteredQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<JobMapResponse>> Handle(GetJobsClusteredQuery request, CancellationToken cancellationToken)
    {
        // Get tradie's blocked suburbs
        var blockedPostcodes = await _context.BlockedSuburbs
            .Where(b => b.TradieProfileId == request.TradieProfileId)
            .Select(b => b.Postcode)
            .ToListAsync(cancellationToken);

        // Get open jobs with location data
        var jobs = await _context.Jobs
            .Include(j => j.TradeCategory)
            .Where(j => j.Status == JobStatus.Open)
            .Where(j => !blockedPostcodes.Contains(j.Postcode))
            .ToListAsync(cancellationToken);

        // Create markers for each job
        var markers = jobs.Select(j => new JobMapMarkerDto(
            JobId: j.Id,
            Title: j.Title,
            Latitude: j.Latitude,
            Longitude: j.Longitude,
            SuburbName: j.SuburbName,
            TradeCategoryName: j.TradeCategory?.Name ?? "Unknown",
            BudgetMax: j.BudgetMax,
            IsUrgent: j.PreferredStartDate.HasValue && j.PreferredStartDate.Value <= DateTime.UtcNow.AddDays(2)
        )).ToList();

        // Create clusters by suburb
        var clusters = jobs
            .GroupBy(j => new { j.Postcode, j.SuburbName, j.Latitude, j.Longitude })
            .Select(g => new JobClusterDto(
                SuburbName: g.Key.SuburbName,
                Postcode: g.Key.Postcode,
                Latitude: g.Key.Latitude,
                Longitude: g.Key.Longitude,
                JobCount: g.Count(),
                UrgentCount: g.Count(j => j.PreferredStartDate.HasValue && j.PreferredStartDate.Value <= DateTime.UtcNow.AddDays(2)),
                TotalBudgetMax: g.Sum(j => j.BudgetMax ?? 0)
            ))
            .OrderByDescending(c => c.JobCount)
            .ToList();

        return Result<JobMapResponse>.Success(new JobMapResponse(markers, clusters));
    }
}
