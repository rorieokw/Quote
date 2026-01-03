using Quote.Domain.Common;
using Quote.Domain.Enums;

namespace Quote.Domain.Entities;

public class JobMedia : BaseEntity
{
    public Guid JobId { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public MediaType MediaType { get; set; }
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? ThumbnailUrl { get; set; }

    // Navigation properties
    public Job Job { get; set; } = null!;
}
