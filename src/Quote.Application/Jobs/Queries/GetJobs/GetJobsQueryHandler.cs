using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Enums;

namespace Quote.Application.Jobs.Queries.GetJobs;

public class GetJobsQueryHandler : IRequestHandler<GetJobsQuery, PaginatedList<JobDto>>
{
    private readonly IApplicationDbContext _context;

    public GetJobsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<JobDto>> Handle(GetJobsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Jobs
            .Include(j => j.TradeCategory)
            .Include(j => j.Customer)
            .Include(j => j.Media)
            .Include(j => j.Quotes)
            .Where(j => j.Status == JobStatus.Open)
            .AsQueryable();

        if (request.TradeCategoryId.HasValue)
        {
            query = query.Where(j => j.TradeCategoryId == request.TradeCategoryId.Value);
        }

        if (request.State.HasValue)
        {
            query = query.Where(j => j.State == request.State.Value);
        }

        if (!string.IsNullOrEmpty(request.Postcode))
        {
            query = query.Where(j => j.Postcode == request.Postcode);
        }

        if (request.MinBudget.HasValue)
        {
            query = query.Where(j => j.BudgetMax >= request.MinBudget.Value);
        }

        if (request.MaxBudget.HasValue)
        {
            query = query.Where(j => j.BudgetMin <= request.MaxBudget.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(j => j.Status == request.Status.Value);
        }

        query = request.SortBy?.ToLower() switch
        {
            "budget" => request.SortDescending
                ? query.OrderByDescending(j => j.BudgetMax)
                : query.OrderBy(j => j.BudgetMin),
            "date" => request.SortDescending
                ? query.OrderByDescending(j => j.PreferredStartDate)
                : query.OrderBy(j => j.PreferredStartDate),
            _ => request.SortDescending
                ? query.OrderByDescending(j => j.CreatedAt)
                : query.OrderBy(j => j.CreatedAt)
        };

        var projectedQuery = query.Select(j => new JobDto
        {
            Id = j.Id,
            Title = j.Title,
            Description = j.Description.Length > 200 ? j.Description.Substring(0, 200) + "..." : j.Description,
            Status = j.Status.ToString(),
            TradeCategory = j.TradeCategory.Name,
            TradeCategoryId = j.TradeCategoryId,
            BudgetMin = j.BudgetMin,
            BudgetMax = j.BudgetMax,
            PreferredStartDate = j.PreferredStartDate,
            SuburbName = j.SuburbName,
            State = j.State.ToString(),
            Postcode = j.Postcode,
            DistanceKm = request.Latitude.HasValue && request.Longitude.HasValue
                ? CalculateDistance(request.Latitude.Value, request.Longitude.Value, j.Latitude, j.Longitude)
                : 0,
            QuoteCount = j.Quotes.Count,
            CustomerName = j.Customer.FirstName,
            CreatedAt = j.CreatedAt,
            MediaUrls = j.Media.OrderBy(m => m.SortOrder).Take(3).Select(m => m.MediaUrl).ToList()
        });

        return await PaginatedList<JobDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize, cancellationToken);
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
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
