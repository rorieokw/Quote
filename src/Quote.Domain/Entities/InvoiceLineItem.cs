using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class InvoiceLineItem : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public string? Unit { get; set; } // "hours", "each", "m2", etc.
    public decimal UnitPrice { get; set; }
    public decimal Amount => Quantity * UnitPrice;
    public bool IsTaxable { get; set; } = true;

    // Navigation
    public Invoice Invoice { get; set; } = null!;
}
