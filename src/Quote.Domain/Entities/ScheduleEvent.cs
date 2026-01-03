using Quote.Domain.Common;
using Quote.Domain.Enums;

namespace Quote.Domain.Entities;

public class ScheduleEvent : BaseEntity
{
    public Guid TradieId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? QuoteId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ScheduleEventType EventType { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsAllDay { get; set; }

    // Location
    public string? Address { get; set; }
    public string? SuburbName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Travel info (for travel events or jobs with travel)
    public int? TravelTimeMinutes { get; set; }
    public double? TravelDistanceKm { get; set; }
    public Guid? PreviousEventId { get; set; } // For linking travel to previous event

    // Recurrence
    public bool IsRecurring { get; set; }
    public Guid? RecurrenceGroupId { get; set; }
    public string? RecurrenceRule { get; set; } // iCal RRULE format

    // Reminders
    public int? ReminderMinutesBefore { get; set; }
    public bool ReminderSent { get; set; }

    // Colors and display
    public string? Color { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public User Tradie { get; set; } = null!;
    public Job? Job { get; set; }
    public JobQuote? Quote { get; set; }
    public ScheduleEvent? PreviousEvent { get; set; }
}
