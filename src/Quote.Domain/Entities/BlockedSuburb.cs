using Quote.Domain.Common;
using Quote.Domain.Enums;

namespace Quote.Domain.Entities;

public class BlockedSuburb : BaseEntity
{
    public Guid TradieProfileId { get; set; }
    public string SuburbName { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
    public AustralianState State { get; set; }
    public string? Reason { get; set; }

    // Navigation properties
    public TradieProfile TradieProfile { get; set; } = null!;
}
