using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class JobAssignment : BaseAuditableEntity
{
    public Guid JobQuoteId { get; set; }
    public Guid TeamMemberId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public JobQuote JobQuote { get; set; } = null!;
    public TeamMember TeamMember { get; set; } = null!;
}
