using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class DisputeEvidence : BaseEntity
{
    public Guid DisputeId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation properties
    public Dispute Dispute { get; set; } = null!;
    public User UploadedByUser { get; set; } = null!;
}
