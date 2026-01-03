using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Application.LeadScoring.Services;
using Quote.Domain.Enums;
using Quote.Shared.DTOs;

namespace Quote.Application.LeadScoring.Queries.GetScoredLeads;

public class GetScoredLeadsQueryHandler : IRequestHandler<GetScoredLeadsQuery, Result<ScoredLeadsResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILeadScoringService _scoringService;

    public GetScoredLeadsQueryHandler(IApplicationDbContext context, ILeadScoringService scoringService)
    {
        _context = context;
        _scoringService = scoringService;
    }

    public async Task<Result<ScoredLeadsResponse>> Handle(GetScoredLeadsQuery request, CancellationToken cancellationToken)
    {
        var tradie = await _context.TradieProfiles
            .FirstOrDefaultAsync(t => t.UserId == request.TradieId, cancellationToken);

        if (tradie == null)
        {
            return Result<ScoredLeadsResponse>.Failure("Tradie profile not found");
        }

        // Refresh scores if requested or if no scores exist
        var hasScores = await _context.LeadScores
            .AnyAsync(ls => ls.TradieId == request.TradieId, cancellationToken);

        if (request.RefreshScores || !hasScores)
        {
            await _scoringService.RecalculateAllScoresForTradieAsync(request.TradieId, cancellationToken);
        }

        // Query scored leads
        var query = _context.LeadScores
            .Include(ls => ls.Job)
                .ThenInclude(j => j.TradeCategory)
            .Include(ls => ls.Job)
                .ThenInclude(j => j.Customer)
            .Include(ls => ls.Job)
                .ThenInclude(j => j.Media)
            .Include(ls => ls.Job)
                .ThenInclude(j => j.Quotes)
            .Where(ls => ls.TradieId == request.TradieId)
            .Where(ls => ls.Job.Status == JobStatus.Open)
            .AsQueryable();

        // Apply filters
        if (request.MinScore.HasValue)
        {
            query = query.Where(ls => ls.TotalScore >= request.MinScore.Value);
        }

        if (!string.IsNullOrEmpty(request.ScoreRating))
        {
            query = request.ScoreRating.ToLower() switch
            {
                "excellent" => query.Where(ls => ls.TotalScore >= 80),
                "good" => query.Where(ls => ls.TotalScore >= 60 && ls.TotalScore < 80),
                "fair" => query.Where(ls => ls.TotalScore >= 40 && ls.TotalScore < 60),
                "low" => query.Where(ls => ls.TotalScore < 40),
                _ => query
            };
        }

        if (request.TradeCategoryId.HasValue)
        {
            query = query.Where(ls => ls.Job.TradeCategoryId == request.TradeCategoryId.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Get customer quality data for the results
        var customerIds = await query.Select(ls => ls.Job.CustomerId).Distinct().ToListAsync(cancellationToken);
        var customerQualities = await _context.CustomerQualities
            .Where(cq => customerIds.Contains(cq.CustomerId))
            .ToDictionaryAsync(cq => cq.CustomerId, cancellationToken);

        // Apply ordering and pagination
        var scoredLeads = await query
            .OrderByDescending(ls => ls.TotalScore)
            .ThenByDescending(ls => ls.Job.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOs
        var leadDtos = scoredLeads.Select(ls =>
        {
            customerQualities.TryGetValue(ls.Job.CustomerId, out var cq);

            return new ScoredLeadDto(
                JobId: ls.JobId,
                Title: ls.Job.Title,
                Description: ls.Job.Description.Length > 200
                    ? ls.Job.Description.Substring(0, 200) + "..."
                    : ls.Job.Description,
                TradeCategory: ls.Job.TradeCategory.Name,
                TradeCategoryIcon: ls.Job.TradeCategory.Icon ?? "",
                BudgetMin: ls.Job.BudgetMin,
                BudgetMax: ls.Job.BudgetMax,
                SuburbName: ls.Job.SuburbName,
                State: ls.Job.State.ToString(),
                Postcode: ls.Job.Postcode,
                PreferredStartDate: ls.Job.PreferredStartDate,
                IsFlexibleDates: ls.Job.IsFlexibleDates,
                CustomerName: ls.Job.Customer.FirstName,
                QuoteCount: ls.Job.Quotes.Count,
                CreatedAt: ls.Job.CreatedAt,
                MediaUrls: ls.Job.Media.OrderBy(m => m.SortOrder).Take(3).Select(m => m.MediaUrl).ToList(),
                TotalScore: ls.TotalScore,
                DistanceScore: ls.DistanceScore,
                BudgetMatchScore: ls.BudgetMatchScore,
                SkillMatchScore: ls.SkillMatchScore,
                CustomerQualityScore: ls.CustomerQualityScore,
                UrgencyScore: ls.UrgencyScore,
                DistanceKm: ls.DistanceKm,
                ScoreRating: _scoringService.GetScoreRating(ls.TotalScore),
                CustomerQuality: cq != null ? MapCustomerQuality(cq) : null
            );
        }).ToList();

        // Calculate stats
        var allScores = await _context.LeadScores
            .Where(ls => ls.TradieId == request.TradieId)
            .Where(ls => ls.Job.Status == JobStatus.Open)
            .Select(ls => ls.TotalScore)
            .ToListAsync(cancellationToken);

        var stats = new LeadScoringStats(
            ExcellentLeads: allScores.Count(s => s >= 80),
            GoodLeads: allScores.Count(s => s >= 60 && s < 80),
            FairLeads: allScores.Count(s => s >= 40 && s < 60),
            LowLeads: allScores.Count(s => s < 40),
            AverageScore: allScores.Any() ? allScores.Average() : 0
        );

        return Result<ScoredLeadsResponse>.Success(new ScoredLeadsResponse(
            Leads: leadDtos,
            TotalCount: totalCount,
            Page: request.PageNumber,
            PageSize: request.PageSize,
            Stats: stats
        ));
    }

    private static LeadCustomerQualityDto MapCustomerQuality(Domain.Entities.CustomerQuality cq)
    {
        var completionRate = cq.TotalJobsPosted > 0
            ? (decimal)cq.JobsCompleted / cq.TotalJobsPosted
            : 0;

        var qualityRating = cq.TotalJobsPosted == 0 ? "New" :
            completionRate >= 0.9m && cq.PaymentReliabilityScore >= 0.9m ? "Excellent" :
            completionRate >= 0.7m && cq.PaymentReliabilityScore >= 0.7m ? "Good" : "Fair";

        return new LeadCustomerQualityDto(
            TotalJobsPosted: cq.TotalJobsPosted,
            JobsCompleted: cq.JobsCompleted,
            JobsCancelled: cq.JobsCancelled,
            CompletionRate: completionRate,
            PaymentReliabilityScore: cq.PaymentReliabilityScore,
            AverageResponseTimeHours: cq.AverageResponseTimeHours,
            QualityRating: qualityRating
        );
    }
}
