using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class MaterialEstimateTemplate : BaseEntity
{
    public Guid TradeCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CalculationFormulaJson { get; set; } = string.Empty;
    public bool IsSystemTemplate { get; set; }
    public Guid? CreatedByTradieId { get; set; }

    // Navigation properties
    public TradeCategory TradeCategory { get; set; } = null!;
    public User? CreatedByTradie { get; set; }
}
