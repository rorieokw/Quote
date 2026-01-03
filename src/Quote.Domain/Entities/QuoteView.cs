using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class QuoteView : BaseEntity
{
    public Guid QuoteId { get; set; }
    public Guid ViewedByUserId { get; set; }
    public DateTime ViewedAt { get; set; }
    public int ViewDurationSeconds { get; set; }

    // Navigation properties
    public JobQuote Quote { get; set; } = null!;
    public User ViewedBy { get; set; } = null!;
}
