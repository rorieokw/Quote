using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class PhotoAnnotation : BaseEntity
{
    public Guid QuoteId { get; set; }
    public Guid OriginalMediaId { get; set; }
    public string AnnotatedImageUrl { get; set; } = string.Empty;
    public string? AnnotationJson { get; set; }  // Store markup data (shapes, text, arrows)

    // Navigation properties
    public JobQuote Quote { get; set; } = null!;
    public JobMedia OriginalMedia { get; set; } = null!;
}
