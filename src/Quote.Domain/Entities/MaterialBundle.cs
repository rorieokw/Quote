using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class MaterialBundle : BaseAuditableEntity
{
    public Guid TradieId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? TradeCategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public int UsageCount { get; set; }

    // Navigation properties
    public User Tradie { get; set; } = null!;
    public TradeCategory? TradeCategory { get; set; }
    public ICollection<MaterialBundleItem> Items { get; set; } = new List<MaterialBundleItem>();
}
