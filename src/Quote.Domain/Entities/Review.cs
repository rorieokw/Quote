using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class Review : BaseAuditableEntity
{
    public Guid JobId { get; set; }
    public Guid ReviewerId { get; set; }
    public Guid RevieweeId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public bool IsFromCustomer { get; set; }

    // Navigation properties
    public Job Job { get; set; } = null!;
    public User Reviewer { get; set; } = null!;
    public User Reviewee { get; set; } = null!;
}
