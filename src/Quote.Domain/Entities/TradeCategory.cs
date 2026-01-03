using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class TradeCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    // Navigation properties
    public ICollection<TradieLicence> Licences { get; set; } = new List<TradieLicence>();
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}
