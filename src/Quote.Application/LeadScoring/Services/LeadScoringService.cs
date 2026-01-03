using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Domain.Entities;
using Quote.Domain.Enums;

namespace Quote.Application.LeadScoring.Services;

public class LeadScoringService : ILeadScoringService
{
    private readonly IApplicationDbContext _context;

    public LeadScoringService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LeadScore> CalculateScoreAsync(Job job, TradieProfile tradie, CustomerQuality? customerQuality, CancellationToken cancellationToken = default)
    {
        var distanceKm = CalculateDistance(tradie.Latitude, tradie.Longitude, job.Latitude, job.Longitude);

        var score = new LeadScore
        {
            Id = Guid.NewGuid(),
            JobId = job.Id,
            TradieId = tradie.UserId,
            DistanceKm = distanceKm,
            DistanceScore = CalculateDistanceScore(distanceKm, tradie.ServiceRadiusKm),
            BudgetMatchScore = CalculateBudgetMatchScore(job, tradie),
            SkillMatchScore = await CalculateSkillMatchScoreAsync(job, tradie, cancellationToken),
            CustomerQualityScore = CalculateCustomerQualityScore(customerQuality),
            UrgencyScore = CalculateUrgencyScore(job),
            CalculatedAt = DateTime.UtcNow
        };

        score.TotalScore = score.DistanceScore + score.BudgetMatchScore + score.SkillMatchScore +
                          score.CustomerQualityScore + score.UrgencyScore;

        return score;
    }

    private int CalculateDistanceScore(double distanceKm, int serviceRadiusKm)
    {
        // Max 25 points for distance
        if (distanceKm <= 5) return 25;
        if (distanceKm <= 10) return 22;
        if (distanceKm <= 15) return 18;
        if (distanceKm <= 20) return 15;
        if (distanceKm <= serviceRadiusKm * 0.5) return 12;
        if (distanceKm <= serviceRadiusKm) return 8;
        if (distanceKm <= serviceRadiusKm * 1.5) return 4;
        return 0; // Outside reasonable range
    }

    private int CalculateBudgetMatchScore(Job job, TradieProfile tradie)
    {
        // Max 25 points for budget match
        if (!tradie.PreferredJobSizeMin.HasValue && !tradie.PreferredJobSizeMax.HasValue)
        {
            return 15; // No preference set = neutral score
        }

        var jobBudget = job.BudgetMax ?? job.BudgetMin ?? 0;

        if (jobBudget == 0)
        {
            return 10; // No budget specified = lower score
        }

        var minPref = tradie.PreferredJobSizeMin ?? 0;
        var maxPref = tradie.PreferredJobSizeMax ?? decimal.MaxValue;

        if (jobBudget >= minPref && jobBudget <= maxPref)
        {
            return 25; // Perfect match
        }

        // Calculate how far outside preference
        if (jobBudget < minPref)
        {
            var ratio = jobBudget / minPref;
            if (ratio >= 0.75m) return 18;
            if (ratio >= 0.5m) return 12;
            return 5;
        }
        else
        {
            var ratio = maxPref / jobBudget;
            if (ratio >= 0.75m) return 18;
            if (ratio >= 0.5m) return 12;
            return 5;
        }
    }

    private async Task<int> CalculateSkillMatchScoreAsync(Job job, TradieProfile tradie, CancellationToken cancellationToken)
    {
        // Max 25 points for skill match
        var hasLicence = await _context.TradieLicences
            .AnyAsync(l => l.TradieProfileId == tradie.Id &&
                          l.TradeCategoryId == job.TradeCategoryId &&
                          l.VerificationStatus == VerificationStatus.Verified &&
                          (l.ExpiryDate == null || l.ExpiryDate > DateTime.UtcNow),
                     cancellationToken);

        if (hasLicence) return 25;

        // Check if tradie has any quotes in this category (experience indicator)
        var hasExperience = await _context.Quotes
            .Include(q => q.Job)
            .AnyAsync(q => q.TradieId == tradie.UserId &&
                          q.Job.TradeCategoryId == job.TradeCategoryId &&
                          q.Status == QuoteStatus.Accepted,
                     cancellationToken);

        if (hasExperience) return 18;

        // Check subscription tier - higher tier tradies can see more categories
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.TradieProfileId == tradie.Id, cancellationToken);

        if (subscription?.Tier == SubscriptionTier.Business) return 15;
        if (subscription?.Tier == SubscriptionTier.Professional) return 12;

        return 8; // Basic eligibility
    }

    private int CalculateCustomerQualityScore(CustomerQuality? quality)
    {
        // Max 15 points for customer quality
        if (quality == null)
        {
            return 8; // New customer = neutral score
        }

        var score = 0;

        // Completion rate (0-6 points)
        if (quality.TotalJobsPosted > 0)
        {
            var completionRate = (decimal)quality.JobsCompleted / quality.TotalJobsPosted;
            score += completionRate switch
            {
                >= 0.9m => 6,
                >= 0.75m => 5,
                >= 0.5m => 3,
                _ => 1
            };
        }
        else
        {
            score += 3; // New customer
        }

        // Payment reliability (0-5 points)
        score += quality.PaymentReliabilityScore switch
        {
            >= 0.95m => 5,
            >= 0.85m => 4,
            >= 0.7m => 3,
            >= 0.5m => 2,
            _ => 0
        };

        // Response time (0-4 points)
        score += quality.AverageResponseTimeHours switch
        {
            <= 2 => 4,
            <= 6 => 3,
            <= 24 => 2,
            <= 48 => 1,
            _ => 0
        };

        return Math.Min(score, 15);
    }

    private int CalculateUrgencyScore(Job job)
    {
        // Max 10 points for urgency
        if (!job.PreferredStartDate.HasValue)
        {
            return job.IsFlexibleDates ? 3 : 5;
        }

        var daysUntilStart = (job.PreferredStartDate.Value - DateTime.UtcNow).TotalDays;

        return daysUntilStart switch
        {
            <= 1 => 10,   // Very urgent
            <= 3 => 8,    // Urgent
            <= 7 => 6,    // This week
            <= 14 => 4,   // Next 2 weeks
            <= 30 => 3,   // This month
            _ => 2        // Future
        };
    }

    public async Task RecalculateAllScoresForTradieAsync(Guid tradieId, CancellationToken cancellationToken = default)
    {
        var tradie = await _context.TradieProfiles
            .Include(t => t.Licences)
            .FirstOrDefaultAsync(t => t.UserId == tradieId, cancellationToken);

        if (tradie == null) return;

        // Get open jobs within service radius
        var jobs = await _context.Jobs
            .Include(j => j.Customer)
            .Include(j => j.TradeCategory)
            .Where(j => j.Status == JobStatus.Open)
            .ToListAsync(cancellationToken);

        // Filter by distance
        var eligibleJobs = jobs.Where(j =>
            CalculateDistance(tradie.Latitude, tradie.Longitude, j.Latitude, j.Longitude) <= tradie.ServiceRadiusKm * 1.5
        ).ToList();

        // Remove old scores
        var oldScores = await _context.LeadScores
            .Where(ls => ls.TradieId == tradieId)
            .ToListAsync(cancellationToken);

        foreach (var oldScore in oldScores)
        {
            _context.LeadScores.Remove(oldScore);
        }

        // Calculate new scores
        foreach (var job in eligibleJobs)
        {
            var customerQuality = await _context.CustomerQualities
                .FirstOrDefaultAsync(cq => cq.CustomerId == job.CustomerId, cancellationToken);

            var score = await CalculateScoreAsync(job, tradie, customerQuality, cancellationToken);
            _context.LeadScores.Add(score);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RecalculateCustomerQualityAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == customerId, cancellationToken);

        if (customer == null) return;

        var jobs = await _context.Jobs
            .Where(j => j.CustomerId == customerId)
            .ToListAsync(cancellationToken);

        var reviews = await _context.Reviews
            .Where(r => r.ReviewerId == customerId)
            .ToListAsync(cancellationToken);

        var payments = await _context.Payments
            .Include(p => p.Milestone)
            .ThenInclude(m => m.Job)
            .Where(p => p.Milestone.Job.CustomerId == customerId)
            .ToListAsync(cancellationToken);

        var quality = await _context.CustomerQualities
            .FirstOrDefaultAsync(cq => cq.CustomerId == customerId, cancellationToken);

        if (quality == null)
        {
            quality = new CustomerQuality
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId
            };
            _context.CustomerQualities.Add(quality);
        }

        quality.TotalJobsPosted = jobs.Count;
        quality.JobsCompleted = jobs.Count(j => j.Status == JobStatus.Completed);
        quality.JobsCancelled = jobs.Count(j => j.Status == JobStatus.Cancelled);
        quality.AverageJobValue = jobs.Any(j => j.BudgetMax.HasValue)
            ? jobs.Where(j => j.BudgetMax.HasValue).Average(j => j.BudgetMax!.Value)
            : 0;
        quality.TotalReviewsGiven = reviews.Count;
        quality.AverageRatingGiven = reviews.Any() ? (decimal)reviews.Average(r => r.Rating) : 0;

        // Payment reliability based on successful vs failed payments
        var successfulPayments = payments.Count(p => p.Status == PaymentStatus.Released);
        var totalPayments = payments.Count;
        quality.PaymentReliabilityScore = totalPayments > 0
            ? (decimal)successfulPayments / totalPayments
            : 1.0m; // New customers get benefit of doubt

        quality.LastCalculatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public string GetScoreRating(int totalScore)
    {
        return totalScore switch
        {
            >= 80 => "Excellent",
            >= 60 => "Good",
            >= 40 => "Fair",
            _ => "Low"
        };
    }

    public List<string> GetScoreExplanations(LeadScore score, Job job, TradieProfile tradie)
    {
        var explanations = new List<string>();

        // Distance explanation
        if (score.DistanceScore >= 22)
            explanations.Add($"Very close to you ({score.DistanceKm:F1}km away)");
        else if (score.DistanceScore >= 15)
            explanations.Add($"Within easy reach ({score.DistanceKm:F1}km away)");
        else if (score.DistanceScore >= 8)
            explanations.Add($"Moderate distance ({score.DistanceKm:F1}km away)");
        else if (score.DistanceScore > 0)
            explanations.Add($"Edge of service area ({score.DistanceKm:F1}km away)");

        // Budget explanation
        if (score.BudgetMatchScore >= 22)
            explanations.Add("Budget perfectly matches your preferences");
        else if (score.BudgetMatchScore >= 15)
            explanations.Add("Budget close to your preferred range");
        else if (score.BudgetMatchScore >= 10)
            explanations.Add("Budget outside your typical range");

        // Skill explanation
        if (score.SkillMatchScore >= 22)
            explanations.Add("You're licensed for this trade category");
        else if (score.SkillMatchScore >= 15)
            explanations.Add("You have experience in this category");

        // Customer quality explanation
        if (score.CustomerQualityScore >= 12)
            explanations.Add("Reliable customer with good history");
        else if (score.CustomerQualityScore >= 8)
            explanations.Add("Customer has decent track record");
        else if (score.CustomerQualityScore < 5)
            explanations.Add("Customer has limited history");

        // Urgency explanation
        if (score.UrgencyScore >= 8)
            explanations.Add("Urgent job - quick response may help");
        else if (score.UrgencyScore >= 5)
            explanations.Add("Job needed within the week");

        return explanations;
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
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
