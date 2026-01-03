using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class QuoteTemplate : BaseAuditableEntity
{
    public Guid TradieId { get; set; }
    public Guid TradeCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DefaultLabourCost { get; set; }
    public decimal? DefaultMaterialsCost { get; set; }
    public int DefaultDurationHours { get; set; }
    public string? DefaultNotes { get; set; }
    public bool IsActive { get; set; } = true;
    public int UsageCount { get; set; }

    // Navigation properties
    public User Tradie { get; set; } = null!;
    public TradeCategory TradeCategory { get; set; } = null!;
    public ICollection<QuoteTemplateMaterial> Materials { get; set; } = new List<QuoteTemplateMaterial>();
}
