using Quote.Domain.Common;
using Quote.Domain.Enums;

namespace Quote.Domain.Entities;

public class Milestone : BaseAuditableEntity
{
    public Guid JobId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public DateTime? DueDate { get; set; }
    public MilestoneStatus Status { get; set; } = MilestoneStatus.Pending;
    public DateTime? CompletedAt { get; set; }
    public int SortOrder { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }

    // Navigation properties
    public Job Job { get; set; } = null!;
    public Payment? Payment { get; set; }
}
