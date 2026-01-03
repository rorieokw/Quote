using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class PortfolioItem : BaseAuditableEntity
{
    public Guid TradieProfileId { get; set; }
    public Guid? CompletedJobId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid TradeCategoryId { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }

    // Navigation properties
    public TradieProfile TradieProfile { get; set; } = null!;
    public Job? CompletedJob { get; set; }
    public TradeCategory TradeCategory { get; set; } = null!;
    public ICollection<PortfolioMedia> Media { get; set; } = new List<PortfolioMedia>();
}
