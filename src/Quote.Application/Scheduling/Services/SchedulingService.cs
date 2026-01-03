using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Domain.Entities;
using Quote.Domain.Enums;
using Quote.Shared.DTOs;

namespace Quote.Application.Scheduling.Services;

public class SchedulingService : ISchedulingService
{
    private readonly IApplicationDbContext _context;
    private readonly IGoogleMapsService _mapsService;

    public SchedulingService(IApplicationDbContext context, IGoogleMapsService mapsService)
    {
        _context = context;
        _mapsService = mapsService;
    }

    public async Task<List<ScheduleEventDto>> GetScheduleForDayAsync(
        Guid tradieId, DateTime date, CancellationToken cancellationToken = default)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var events = await _context.ScheduleEvents
            .Include(e => e.Job)
            .Include(e => e.Quote)
            .Where(e => e.TradieId == tradieId)
            .Where(e => e.StartTime < dayEnd && e.EndTime > dayStart)
            .OrderBy(e => e.StartTime)
            .ToListAsync(cancellationToken);

        return events.Select(MapToDto).ToList();
    }

    public async Task<ScheduleWeekResponse> GetScheduleForWeekAsync(
        Guid tradieId, DateTime weekStart, CancellationToken cancellationToken = default)
    {
        var start = weekStart.Date;
        while (start.DayOfWeek != DayOfWeek.Monday)
            start = start.AddDays(-1);

        var end = start.AddDays(7);

        var events = await _context.ScheduleEvents
            .Include(e => e.Job)
            .Include(e => e.Quote)
            .Where(e => e.TradieId == tradieId)
            .Where(e => e.StartTime < end && e.EndTime > start)
            .OrderBy(e => e.StartTime)
            .ToListAsync(cancellationToken);

        var days = new List<ScheduleDayResponse>();
        for (int i = 0; i < 7; i++)
        {
            var day = start.AddDays(i);
            var dayEvents = events
                .Where(e => e.StartTime.Date == day || e.EndTime.Date == day || (e.StartTime < day && e.EndTime > day.AddDays(1)))
                .ToList();

            days.Add(new ScheduleDayResponse(
                Date: day,
                Events: dayEvents.Select(MapToDto).ToList(),
                TotalTravelMinutes: dayEvents.Where(e => e.EventType == ScheduleEventType.Travel).Sum(e => e.TravelTimeMinutes ?? 0) +
                                   dayEvents.Where(e => e.TravelTimeMinutes.HasValue).Sum(e => e.TravelTimeMinutes!.Value),
                TotalTravelKm: dayEvents.Sum(e => e.TravelDistanceKm ?? 0),
                JobCount: dayEvents.Count(e => e.EventType == ScheduleEventType.Job),
                SiteVisitCount: dayEvents.Count(e => e.EventType == ScheduleEventType.SiteVisit)
            ));
        }

        var totalRevenue = await CalculateEstimatedRevenueAsync(events.Where(e => e.JobId.HasValue).Select(e => e.JobId!.Value).Distinct().ToList(), cancellationToken);

        return new ScheduleWeekResponse(
            WeekStart: start,
            WeekEnd: end.AddDays(-1),
            Days: days,
            Stats: new ScheduleWeekStats(
                TotalEvents: events.Count,
                TotalJobs: events.Count(e => e.EventType == ScheduleEventType.Job),
                TotalSiteVisits: events.Count(e => e.EventType == ScheduleEventType.SiteVisit),
                TotalTravelMinutes: events.Sum(e => e.TravelTimeMinutes ?? 0),
                TotalTravelKm: events.Sum(e => e.TravelDistanceKm ?? 0),
                EstimatedRevenue: totalRevenue
            )
        );
    }

    public async Task<ScheduleMonthResponse> GetScheduleForMonthAsync(
        Guid tradieId, int year, int month, CancellationToken cancellationToken = default)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);

        var events = await _context.ScheduleEvents
            .Include(e => e.Job)
            .Include(e => e.Quote)
            .Where(e => e.TradieId == tradieId)
            .Where(e => e.StartTime < end && e.EndTime > start)
            .OrderBy(e => e.StartTime)
            .ToListAsync(cancellationToken);

        var days = new List<ScheduleDayResponse>();
        var daysInMonth = DateTime.DaysInMonth(year, month);

        for (int i = 1; i <= daysInMonth; i++)
        {
            var day = new DateTime(year, month, i);
            var dayEvents = events.Where(e => e.StartTime.Date == day).ToList();

            days.Add(new ScheduleDayResponse(
                Date: day,
                Events: dayEvents.Select(MapToDto).ToList(),
                TotalTravelMinutes: dayEvents.Sum(e => e.TravelTimeMinutes ?? 0),
                TotalTravelKm: dayEvents.Sum(e => e.TravelDistanceKm ?? 0),
                JobCount: dayEvents.Count(e => e.EventType == ScheduleEventType.Job),
                SiteVisitCount: dayEvents.Count(e => e.EventType == ScheduleEventType.SiteVisit)
            ));
        }

        var eventsByType = events
            .GroupBy(e => e.EventType.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var totalRevenue = await CalculateEstimatedRevenueAsync(
            events.Where(e => e.JobId.HasValue).Select(e => e.JobId!.Value).Distinct().ToList(),
            cancellationToken);

        return new ScheduleMonthResponse(
            Year: year,
            Month: month,
            Days: days,
            Stats: new ScheduleMonthStats(
                TotalWorkDays: days.Count(d => d.JobCount > 0 || d.SiteVisitCount > 0),
                TotalJobs: events.Count(e => e.EventType == ScheduleEventType.Job),
                TotalSiteVisits: events.Count(e => e.EventType == ScheduleEventType.SiteVisit),
                TotalTravelMinutes: events.Sum(e => e.TravelTimeMinutes ?? 0),
                TotalTravelKm: events.Sum(e => e.TravelDistanceKm ?? 0),
                EstimatedRevenue: totalRevenue,
                EventsByType: eventsByType
            )
        );
    }

    public async Task<ScheduleEvent> CreateEventAsync(
        Guid tradieId, CreateScheduleEventRequest request, CancellationToken cancellationToken = default)
    {
        var scheduleEvent = new ScheduleEvent
        {
            Id = Guid.NewGuid(),
            TradieId = tradieId,
            Title = request.Title,
            Description = request.Description,
            EventType = Enum.Parse<ScheduleEventType>(request.EventType),
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsAllDay = request.IsAllDay,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            JobId = request.JobId,
            QuoteId = request.QuoteId,
            Color = request.Color ?? GetDefaultColor(request.EventType),
            Notes = request.Notes,
            ReminderMinutesBefore = request.ReminderMinutesBefore,
            RecurrenceRule = request.RecurrenceRule,
            IsRecurring = !string.IsNullOrEmpty(request.RecurrenceRule),
            CreatedAt = DateTime.UtcNow
        };

        // Get suburb name from address if location provided
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            var geocode = await _mapsService.ReverseGeocodeAsync(request.Latitude.Value, request.Longitude.Value, cancellationToken);
            if (geocode != null)
            {
                scheduleEvent.SuburbName = geocode.Suburb;
            }
        }

        _context.ScheduleEvents.Add(scheduleEvent);
        await _context.SaveChangesAsync(cancellationToken);

        // Calculate travel time from previous event
        await CalculateTravelTimesAsync(tradieId, scheduleEvent.StartTime.Date, cancellationToken);

        return scheduleEvent;
    }

    public async Task<ScheduleEvent?> UpdateEventAsync(
        Guid tradieId, UpdateScheduleEventRequest request, CancellationToken cancellationToken = default)
    {
        var scheduleEvent = await _context.ScheduleEvents
            .FirstOrDefaultAsync(e => e.Id == request.Id && e.TradieId == tradieId, cancellationToken);

        if (scheduleEvent == null)
            return null;

        if (request.Title != null)
            scheduleEvent.Title = request.Title;
        if (request.Description != null)
            scheduleEvent.Description = request.Description;
        if (request.StartTime.HasValue)
            scheduleEvent.StartTime = request.StartTime.Value;
        if (request.EndTime.HasValue)
            scheduleEvent.EndTime = request.EndTime.Value;
        if (request.IsAllDay.HasValue)
            scheduleEvent.IsAllDay = request.IsAllDay.Value;
        if (request.Address != null)
            scheduleEvent.Address = request.Address;
        if (request.Latitude.HasValue)
            scheduleEvent.Latitude = request.Latitude;
        if (request.Longitude.HasValue)
            scheduleEvent.Longitude = request.Longitude;
        if (request.Color != null)
            scheduleEvent.Color = request.Color;
        if (request.Notes != null)
            scheduleEvent.Notes = request.Notes;
        if (request.ReminderMinutesBefore.HasValue)
            scheduleEvent.ReminderMinutesBefore = request.ReminderMinutesBefore;

        scheduleEvent.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Recalculate travel times
        await CalculateTravelTimesAsync(tradieId, scheduleEvent.StartTime.Date, cancellationToken);

        return scheduleEvent;
    }

    public async Task<bool> DeleteEventAsync(
        Guid tradieId, Guid eventId, bool deleteRecurrenceSeries = false, CancellationToken cancellationToken = default)
    {
        var scheduleEvent = await _context.ScheduleEvents
            .FirstOrDefaultAsync(e => e.Id == eventId && e.TradieId == tradieId, cancellationToken);

        if (scheduleEvent == null)
            return false;

        if (deleteRecurrenceSeries && scheduleEvent.RecurrenceGroupId.HasValue)
        {
            var seriesEvents = await _context.ScheduleEvents
                .Where(e => e.RecurrenceGroupId == scheduleEvent.RecurrenceGroupId && e.TradieId == tradieId)
                .ToListAsync(cancellationToken);

            foreach (var ev in seriesEvents)
            {
                _context.ScheduleEvents.Remove(ev);
            }
        }
        else
        {
            _context.ScheduleEvents.Remove(scheduleEvent);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<OptimizedScheduleResponse> OptimizeDayScheduleAsync(
        Guid tradieId, OptimizeDayRequest request, CancellationToken cancellationToken = default)
    {
        var dayStart = request.Date.Date;
        var dayEnd = dayStart.AddDays(1);

        var events = await _context.ScheduleEvents
            .Where(e => e.TradieId == tradieId)
            .Where(e => e.StartTime < dayEnd && e.EndTime > dayStart)
            .Where(e => e.EventType == ScheduleEventType.Job || e.EventType == ScheduleEventType.SiteVisit)
            .Where(e => e.Latitude.HasValue && e.Longitude.HasValue)
            .OrderBy(e => e.StartTime)
            .ToListAsync(cancellationToken);

        if (events.Count <= 1)
        {
            return new OptimizedScheduleResponse(
                Date: request.Date,
                OptimizedEvents: events.Select(MapToDto).ToList(),
                TotalTravelMinutes: 0,
                OriginalTravelMinutes: events.Sum(e => e.TravelTimeMinutes ?? 0),
                MinutesSaved: 0,
                TotalTravelKm: 0,
                OptimizationNotes: "No optimization needed for single event"
            );
        }

        // Calculate original travel time
        var originalTravelMinutes = 0;
        var currentLat = request.StartLatitude;
        var currentLng = request.StartLongitude;

        foreach (var ev in events)
        {
            var distance = await _mapsService.GetDistanceMatrixAsync(
                (currentLat, currentLng),
                (ev.Latitude!.Value, ev.Longitude!.Value),
                cancellationToken);

            originalTravelMinutes += distance.Elements.FirstOrDefault()?.DurationSeconds / 60 ?? 0;
            currentLat = ev.Latitude!.Value;
            currentLng = ev.Longitude!.Value;
        }

        // Optimize using nearest neighbor algorithm
        var optimizedOrder = new List<ScheduleEvent>();
        var remaining = new List<ScheduleEvent>(events);
        currentLat = request.StartLatitude;
        currentLng = request.StartLongitude;

        while (remaining.Any())
        {
            ScheduleEvent? nearest = null;
            int nearestDuration = int.MaxValue;

            foreach (var ev in remaining)
            {
                var distance = await _mapsService.GetDistanceMatrixAsync(
                    (currentLat, currentLng),
                    (ev.Latitude!.Value, ev.Longitude!.Value),
                    cancellationToken);

                var duration = distance.Elements.FirstOrDefault()?.DurationSeconds ?? int.MaxValue;
                if (duration < nearestDuration)
                {
                    nearestDuration = duration;
                    nearest = ev;
                }
            }

            if (nearest != null)
            {
                optimizedOrder.Add(nearest);
                remaining.Remove(nearest);
                currentLat = nearest.Latitude!.Value;
                currentLng = nearest.Longitude!.Value;
            }
        }

        // Calculate optimized travel time and update event times
        var workDayStart = request.WorkDayStart ?? TimeSpan.FromHours(8);
        var currentTime = request.Date.Date.Add(workDayStart);
        currentLat = request.StartLatitude;
        currentLng = request.StartLongitude;
        var totalOptimizedTravel = 0;
        var totalDistance = 0.0;

        foreach (var ev in optimizedOrder)
        {
            var distance = await _mapsService.GetDistanceMatrixAsync(
                (currentLat, currentLng),
                (ev.Latitude!.Value, ev.Longitude!.Value),
                cancellationToken);

            var element = distance.Elements.FirstOrDefault();
            var travelMinutes = (element?.DurationInTrafficSeconds ?? element?.DurationSeconds ?? 0) / 60;
            var travelKm = (element?.DistanceMeters ?? 0) / 1000.0;

            totalOptimizedTravel += travelMinutes;
            totalDistance += travelKm;

            ev.TravelTimeMinutes = travelMinutes;
            ev.TravelDistanceKm = travelKm;

            var eventDuration = ev.EndTime - ev.StartTime;
            ev.StartTime = currentTime.AddMinutes(travelMinutes);
            ev.EndTime = ev.StartTime.Add(eventDuration);

            currentTime = ev.EndTime;
            if (request.BreakDurationMinutes.HasValue && optimizedOrder.IndexOf(ev) == optimizedOrder.Count / 2)
            {
                currentTime = currentTime.AddMinutes(request.BreakDurationMinutes.Value);
            }

            currentLat = ev.Latitude!.Value;
            currentLng = ev.Longitude!.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new OptimizedScheduleResponse(
            Date: request.Date,
            OptimizedEvents: optimizedOrder.Select(MapToDto).ToList(),
            TotalTravelMinutes: totalOptimizedTravel,
            OriginalTravelMinutes: originalTravelMinutes,
            MinutesSaved: originalTravelMinutes - totalOptimizedTravel,
            TotalTravelKm: totalDistance,
            OptimizationNotes: $"Optimized {events.Count} stops, saved {originalTravelMinutes - totalOptimizedTravel} minutes of travel"
        );
    }

    public async Task<List<ScheduleConflictDto>> GetConflictsAsync(
        Guid tradieId, DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        var events = await _context.ScheduleEvents
            .Where(e => e.TradieId == tradieId)
            .Where(e => e.StartTime < end && e.EndTime > start)
            .Where(e => !e.IsAllDay)
            .OrderBy(e => e.StartTime)
            .ToListAsync(cancellationToken);

        var conflicts = new List<ScheduleConflictDto>();

        for (int i = 0; i < events.Count; i++)
        {
            for (int j = i + 1; j < events.Count; j++)
            {
                var ev1 = events[i];
                var ev2 = events[j];

                if (ev1.EndTime > ev2.StartTime && ev1.StartTime < ev2.EndTime)
                {
                    var conflictType = ev1.StartTime == ev2.StartTime && ev1.EndTime == ev2.EndTime
                        ? "Exact overlap"
                        : "Partial overlap";

                    conflicts.Add(new ScheduleConflictDto(
                        Event1Id: ev1.Id,
                        Event1Title: ev1.Title,
                        Event1Start: ev1.StartTime,
                        Event1End: ev1.EndTime,
                        Event2Id: ev2.Id,
                        Event2Title: ev2.Title,
                        Event2Start: ev2.StartTime,
                        Event2End: ev2.EndTime,
                        ConflictType: conflictType
                    ));
                }
            }
        }

        return conflicts;
    }

    public async Task<ScheduleAvailabilityResponse> GetAvailableSlotsAsync(
        Guid tradieId, ScheduleAvailabilityRequest request, CancellationToken cancellationToken = default)
    {
        var dayStart = request.Date.Date.AddHours(8); // 8 AM
        var dayEnd = request.Date.Date.AddHours(17);  // 5 PM

        var events = await _context.ScheduleEvents
            .Where(e => e.TradieId == tradieId)
            .Where(e => e.StartTime.Date == request.Date.Date)
            .Where(e => !e.IsAllDay)
            .OrderBy(e => e.StartTime)
            .ToListAsync(cancellationToken);

        var slots = new List<AvailableSlotDto>();
        var currentTime = dayStart;

        foreach (var ev in events)
        {
            if (ev.StartTime > currentTime)
            {
                var gap = (ev.StartTime - currentTime).TotalMinutes;
                if (gap >= request.DurationMinutes)
                {
                    var quality = DetermineSlotQuality(gap, request.DurationMinutes, ev, events, request.CustomerLatitude, request.CustomerLongitude);
                    slots.Add(new AvailableSlotDto(
                        StartTime: currentTime,
                        EndTime: ev.StartTime,
                        TravelTimeFromPrevious: null,
                        TravelTimeToNext: null,
                        Quality: quality
                    ));
                }
            }
            currentTime = ev.EndTime > currentTime ? ev.EndTime : currentTime;
        }

        // Check for slot after last event
        if (dayEnd > currentTime)
        {
            var gap = (dayEnd - currentTime).TotalMinutes;
            if (gap >= request.DurationMinutes)
            {
                slots.Add(new AvailableSlotDto(
                    StartTime: currentTime,
                    EndTime: dayEnd,
                    TravelTimeFromPrevious: null,
                    TravelTimeToNext: null,
                    Quality: "Good"
                ));
            }
        }

        return new ScheduleAvailabilityResponse(
            Date: request.Date,
            AvailableSlots: slots
        );
    }

    public async Task<ScheduleEvent?> QuickAddJobAsync(
        Guid tradieId, QuickAddJobRequest request, CancellationToken cancellationToken = default)
    {
        var job = await _context.Jobs
            .Include(j => j.TradeCategory)
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (job == null)
            return null;

        var startTime = request.PreferredDate.Date.Add(request.PreferredTime ?? TimeSpan.FromHours(9));

        var createRequest = new CreateScheduleEventRequest(
            Title: $"Job: {job.Title}",
            Description: job.Description,
            EventType: "Job",
            StartTime: startTime,
            EndTime: startTime.AddMinutes(request.EstimatedDurationMinutes),
            IsAllDay: false,
            Address: $"{job.SuburbName}, {job.State}",
            Latitude: job.Latitude,
            Longitude: job.Longitude,
            JobId: job.Id,
            QuoteId: null,
            Color: "#2563eb",
            Notes: null,
            ReminderMinutesBefore: 30,
            RecurrenceRule: null
        );

        return await CreateEventAsync(tradieId, createRequest, cancellationToken);
    }

    public async Task CalculateTravelTimesAsync(Guid tradieId, DateTime date, CancellationToken cancellationToken = default)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var events = await _context.ScheduleEvents
            .Where(e => e.TradieId == tradieId)
            .Where(e => e.StartTime < dayEnd && e.EndTime > dayStart)
            .Where(e => e.Latitude.HasValue && e.Longitude.HasValue)
            .OrderBy(e => e.StartTime)
            .ToListAsync(cancellationToken);

        if (events.Count < 2)
            return;

        var tradie = await _context.TradieProfiles
            .FirstOrDefaultAsync(t => t.UserId == tradieId, cancellationToken);

        if (tradie == null)
            return;

        var previousLat = tradie.Latitude;
        var previousLng = tradie.Longitude;

        foreach (var ev in events)
        {
            var distance = await _mapsService.GetDistanceMatrixAsync(
                (previousLat, previousLng),
                (ev.Latitude!.Value, ev.Longitude!.Value),
                cancellationToken);

            var element = distance.Elements.FirstOrDefault();
            if (element != null)
            {
                ev.TravelTimeMinutes = (element.DurationInTrafficSeconds ?? element.DurationSeconds) / 60;
                ev.TravelDistanceKm = element.DistanceMeters / 1000.0;
            }

            previousLat = ev.Latitude!.Value;
            previousLng = ev.Longitude!.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    #region Private Helpers

    private ScheduleEventDto MapToDto(ScheduleEvent ev)
    {
        return new ScheduleEventDto(
            Id: ev.Id,
            Title: ev.Title,
            Description: ev.Description,
            EventType: ev.EventType.ToString(),
            StartTime: ev.StartTime,
            EndTime: ev.EndTime,
            IsAllDay: ev.IsAllDay,
            Address: ev.Address,
            SuburbName: ev.SuburbName,
            Latitude: ev.Latitude,
            Longitude: ev.Longitude,
            TravelTimeMinutes: ev.TravelTimeMinutes,
            TravelDistanceKm: ev.TravelDistanceKm,
            Color: ev.Color,
            Notes: ev.Notes,
            JobId: ev.JobId,
            JobTitle: ev.Job?.Title,
            QuoteId: ev.QuoteId,
            IsRecurring: ev.IsRecurring
        );
    }

    private async Task<decimal> CalculateEstimatedRevenueAsync(List<Guid> jobIds, CancellationToken cancellationToken)
    {
        if (!jobIds.Any())
            return 0;

        var quotes = await _context.Quotes
            .Where(q => jobIds.Contains(q.JobId))
            .Where(q => q.Status == QuoteStatus.Accepted)
            .ToListAsync(cancellationToken);

        return quotes.Sum(q => q.TotalCost);
    }

    private string GetDefaultColor(string eventType)
    {
        return eventType switch
        {
            "Job" => "#2563eb",       // Blue
            "SiteVisit" => "#10b981", // Green
            "Meeting" => "#8b5cf6",   // Purple
            "Travel" => "#6b7280",    // Gray
            "Break" => "#f59e0b",     // Amber
            "Personal" => "#ec4899",  // Pink
            "Blocked" => "#ef4444",   // Red
            _ => "#6b7280"            // Gray
        };
    }

    private string DetermineSlotQuality(double gapMinutes, int requiredMinutes, ScheduleEvent nextEvent, List<ScheduleEvent> allEvents, double? customerLat, double? customerLng)
    {
        // More buffer time = better quality
        var buffer = gapMinutes - requiredMinutes;

        if (buffer >= 60)
            return "Excellent";
        if (buffer >= 30)
            return "Good";
        return "Fair";
    }

    #endregion
}
