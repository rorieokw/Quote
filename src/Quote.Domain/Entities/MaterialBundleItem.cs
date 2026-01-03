using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class MaterialBundleItem : BaseEntity
{
    public Guid BundleId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? SupplierName { get; set; }
    public string? ProductUrl { get; set; }
    public decimal DefaultQuantity { get; set; }
    public string? Unit { get; set; }
    public decimal EstimatedUnitPrice { get; set; }
    public int SortOrder { get; set; }
    public decimal TotalPrice => DefaultQuantity * EstimatedUnitPrice;

    // Navigation properties
    public MaterialBundle Bundle { get; set; } = null!;
}
