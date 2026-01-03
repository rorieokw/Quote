using Quote.Domain.Common;
using Quote.Domain.Enums;

namespace Quote.Domain.Entities;

public class PortfolioMedia : BaseEntity
{
    public Guid PortfolioItemId { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
    public bool IsBefore { get; set; }  // true = before photo, false = after photo
    public string? Caption { get; set; }
    public int SortOrder { get; set; }

    // Navigation properties
    public PortfolioItem PortfolioItem { get; set; } = null!;
}
