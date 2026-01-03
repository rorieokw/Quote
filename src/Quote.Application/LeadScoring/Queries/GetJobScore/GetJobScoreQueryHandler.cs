using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Application.LeadScoring.Services;
using Quote.Shared.DTOs;

namespace Quote.Application.LeadScoring.Queries.GetJobScore;

public class GetJobScoreQueryHandler : IRequestHandler<GetJobScoreQuery, Result<LeadScoreDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILeadScoringService _scoringService;

    public GetJobScoreQueryHandler(IApplicationDbContext context, ILeadScoringService scoringService)
    {
        _context = context;
        _scoringService = scoringService;
    }

    public async Task<Result<LeadScoreDetailDto>> Handle(GetJobScoreQuery request, CancellationToken cancellationToken)
    {
        var tradie = await _context.TradieProfiles
            .Include(t => t.Licences)
            .FirstOrDefaultAsync(t => t.UserId == request.TradieId, cancellationToken);

        if (tradie == null)
        {
            return Result<LeadScoreDetailDto>.Failure("Tradie profile not found");
        }

        var job = await _context.Jobs
            .Include(j => j.TradeCategory)
            .Include(j => j.Customer)
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (job == null)
        {
            return Result<LeadScoreDetailDto>.Failure("Job not found");
        }

        // Try to get existing score or calculate new one
        var score = await _context.LeadScores
            .FirstOrDefaultAsync(ls => ls.TradieId == request.TradieId && ls.JobId == request.JobId, cancellationToken);

        if (score == null)
        {
            var customerQuality = await _context.CustomerQualities
                .FirstOrDefaultAsync(cq => cq.CustomerId == job.CustomerId, cancellationToken);

            score = await _scoringService.CalculateScoreAsync(job, tradie, customerQuality, cancellationToken);
            _context.LeadScores.Add(score);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var explanations = _scoringService.GetScoreExplanations(score, job, tradie);

        return Result<LeadScoreDetailDto>.Success(new LeadScoreDetailDto(
            JobId: score.JobId,
            TotalScore: score.TotalScore,
            DistanceScore: score.DistanceScore,
            BudgetMatchScore: score.BudgetMatchScore,
            SkillMatchScore: score.SkillMatchScore,
            CustomerQualityScore: score.CustomerQualityScore,
            UrgencyScore: score.UrgencyScore,
            DistanceKm: score.DistanceKm,
            ScoreRating: _scoringService.GetScoreRating(score.TotalScore),
            ScoreExplanations: explanations
        ));
    }
}
