using Quote.Domain.Entities;
using Quote.Shared.DTOs;

namespace Quote.Application.Scheduling.Services;

public interface ISchedulingService
{
    Task<List<ScheduleEventDto>> GetScheduleForDayAsync(Guid tradieId, DateTime date, CancellationToken cancellationToken = default);
    Task<ScheduleWeekResponse> GetScheduleForWeekAsync(Guid tradieId, DateTime weekStart, CancellationToken cancellationToken = default);
    Task<ScheduleMonthResponse> GetScheduleForMonthAsync(Guid tradieId, int year, int month, CancellationToken cancellationToken = default);

    Task<ScheduleEvent> CreateEventAsync(Guid tradieId, CreateScheduleEventRequest request, CancellationToken cancellationToken = default);
    Task<ScheduleEvent?> UpdateEventAsync(Guid tradieId, UpdateScheduleEventRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteEventAsync(Guid tradieId, Guid eventId, bool deleteRecurrenceSeries = false, CancellationToken cancellationToken = default);

    Task<OptimizedScheduleResponse> OptimizeDayScheduleAsync(Guid tradieId, OptimizeDayRequest request, CancellationToken cancellationToken = default);
    Task<List<ScheduleConflictDto>> GetConflictsAsync(Guid tradieId, DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task<ScheduleAvailabilityResponse> GetAvailableSlotsAsync(Guid tradieId, ScheduleAvailabilityRequest request, CancellationToken cancellationToken = default);

    Task<ScheduleEvent?> QuickAddJobAsync(Guid tradieId, QuickAddJobRequest request, CancellationToken cancellationToken = default);
    Task CalculateTravelTimesAsync(Guid tradieId, DateTime date, CancellationToken cancellationToken = default);
}
