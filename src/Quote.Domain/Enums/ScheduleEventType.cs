namespace Quote.Domain.Enums;

public enum ScheduleEventType
{
    Job,           // Scheduled job work
    SiteVisit,     // Site inspection/quote visit
    Meeting,       // Customer or business meeting
    Travel,        // Travel time between jobs
    Break,         // Lunch/break time
    Personal,      // Personal time off
    Blocked,       // Unavailable time
    Other
}
