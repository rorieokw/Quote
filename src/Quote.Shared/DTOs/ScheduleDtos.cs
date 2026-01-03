namespace Quote.Shared.DTOs;

public record ScheduleEventDto(
    Guid Id,
    string Title,
    string? Description,
    string EventType,
    DateTime StartTime,
    DateTime EndTime,
    bool IsAllDay,
    string? Address,
    string? SuburbName,
    double? Latitude,
    double? Longitude,
    int? TravelTimeMinutes,
    double? TravelDistanceKm,
    string? Color,
    string? Notes,
    Guid? JobId,
    string? JobTitle,
    Guid? QuoteId,
    bool IsRecurring
);

public record CreateScheduleEventRequest(
    string Title,
    string? Description,
    string EventType,
    DateTime StartTime,
    DateTime EndTime,
    bool IsAllDay,
    string? Address,
    double? Latitude,
    double? Longitude,
    Guid? JobId,
    Guid? QuoteId,
    string? Color,
    string? Notes,
    int? ReminderMinutesBefore,
    string? RecurrenceRule
);

public record UpdateScheduleEventRequest(
    Guid Id,
    string? Title,
    string? Description,
    DateTime? StartTime,
    DateTime? EndTime,
    bool? IsAllDay,
    string? Address,
    double? Latitude,
    double? Longitude,
    string? Color,
    string? Notes,
    int? ReminderMinutesBefore,
    bool? UpdateRecurrenceSeries
);

public record ScheduleDayResponse(
    DateTime Date,
    List<ScheduleEventDto> Events,
    int TotalTravelMinutes,
    double TotalTravelKm,
    int JobCount,
    int SiteVisitCount
);

public record ScheduleWeekResponse(
    DateTime WeekStart,
    DateTime WeekEnd,
    List<ScheduleDayResponse> Days,
    ScheduleWeekStats Stats
);

public record ScheduleWeekStats(
    int TotalEvents,
    int TotalJobs,
    int TotalSiteVisits,
    int TotalTravelMinutes,
    double TotalTravelKm,
    decimal EstimatedRevenue
);

public record ScheduleMonthResponse(
    int Year,
    int Month,
    List<ScheduleDayResponse> Days,
    ScheduleMonthStats Stats
);

public record ScheduleMonthStats(
    int TotalWorkDays,
    int TotalJobs,
    int TotalSiteVisits,
    int TotalTravelMinutes,
    double TotalTravelKm,
    decimal EstimatedRevenue,
    Dictionary<string, int> EventsByType
);

public record OptimizeDayRequest(
    DateTime Date,
    double StartLatitude,
    double StartLongitude,
    TimeSpan? WorkDayStart,
    TimeSpan? WorkDayEnd,
    int? BreakDurationMinutes
);

public record OptimizedScheduleResponse(
    DateTime Date,
    List<ScheduleEventDto> OptimizedEvents,
    int TotalTravelMinutes,
    int OriginalTravelMinutes,
    int MinutesSaved,
    double TotalTravelKm,
    string OptimizationNotes
);

public record ScheduleConflictDto(
    Guid Event1Id,
    string Event1Title,
    DateTime Event1Start,
    DateTime Event1End,
    Guid Event2Id,
    string Event2Title,
    DateTime Event2Start,
    DateTime Event2End,
    string ConflictType
);

public record ScheduleAvailabilityRequest(
    DateTime Date,
    int DurationMinutes,
    double? CustomerLatitude,
    double? CustomerLongitude
);

public record ScheduleAvailabilityResponse(
    DateTime Date,
    List<AvailableSlotDto> AvailableSlots
);

public record AvailableSlotDto(
    DateTime StartTime,
    DateTime EndTime,
    int? TravelTimeFromPrevious,
    int? TravelTimeToNext,
    string Quality // "Excellent", "Good", "Fair" based on travel time
);

public record QuickAddJobRequest(
    Guid JobId,
    DateTime PreferredDate,
    TimeSpan? PreferredTime,
    int EstimatedDurationMinutes
);

public record CalendarDayDto(
    DateTime Date,
    bool IsToday,
    bool IsCurrentMonth,
    bool HasEvents,
    int EventCount,
    List<CalendarEventSummaryDto> Events
);

public record CalendarEventSummaryDto(
    Guid Id,
    string Title,
    string EventType,
    TimeSpan StartTime,
    TimeSpan EndTime,
    string Color,
    bool IsAllDay
);
