namespace Quote.Shared.DTOs;

// Tradie Profile
public record TradieProfileDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string? BusinessName,
    string? Bio,
    decimal? HourlyRate,
    int ServiceRadiusKm,
    decimal Rating,
    int TotalJobsCompleted,
    int TotalReviews,
    bool IsAvailableNow,
    DateTime? AvailableNowUntil,
    VerificationStatusDto VerificationStatus,
    List<string> TradeCategories
);

// Verification Badge
public record VerificationStatusDto(
    bool IsLicensed,
    bool IsInsured,
    bool IsPoliceChecked,
    bool HasValidSubscription,
    string VerificationLevel,  // None, Basic, Verified, Premium
    List<string> VerifiedBadges
);

// Job Preferences
public record JobPreferencesDto(
    decimal? MinJobSize,
    decimal? MaxJobSize,
    int ServiceRadiusKm,
    List<BlockedSuburbDto> BlockedSuburbs
);

public record UpdateJobPreferencesRequest(
    decimal? MinJobSize,
    decimal? MaxJobSize,
    int ServiceRadiusKm
);

// Blocked Suburbs
public record BlockedSuburbDto(
    Guid Id,
    string SuburbName,
    string Postcode,
    string State,
    string? Reason
);

public record BlockSuburbRequest(
    string SuburbName,
    string Postcode,
    string State,
    string? Reason
);

// Available Now
public record AvailableNowDto(
    bool IsAvailableNow,
    DateTime? AvailableUntil
);

public record SetAvailableNowRequest(
    bool IsAvailable,
    int? HoursAvailable  // How many hours from now (null = until midnight)
);

// Customer Quality (for tradies viewing customer reliability)
public record CustomerQualityDto(
    Guid CustomerId,
    string CustomerName,
    int TotalJobsPosted,
    int JobsCompleted,
    int JobsCancelled,
    decimal CompletionRate,
    int QuotesRequestedLast30Days,
    int QuotesAcceptedLast30Days,
    decimal AcceptanceRate,
    bool IsTyreKicker,
    decimal? AverageJobValue,
    double? AverageTimeToAcceptHours
);
