using Quote.Domain.Entities;

namespace Quote.Application.LeadScoring.Services;

public interface ILeadScoringService
{
    Task<LeadScore> CalculateScoreAsync(Job job, TradieProfile tradie, CustomerQuality? customerQuality, CancellationToken cancellationToken = default);
    Task RecalculateAllScoresForTradieAsync(Guid tradieId, CancellationToken cancellationToken = default);
    Task RecalculateCustomerQualityAsync(Guid customerId, CancellationToken cancellationToken = default);
    string GetScoreRating(int totalScore);
    List<string> GetScoreExplanations(LeadScore score, Job job, TradieProfile tradie);
}
