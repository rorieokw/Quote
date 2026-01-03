using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class QuoteMaterial : BaseEntity
{
    public Guid QuoteId { get; set; }
    public string? SupplierProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductUrl { get; set; }
    public string? SupplierName { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;

    // Navigation properties
    public JobQuote Quote { get; set; } = null!;
}
