using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class QuoteTemplateMaterial : BaseEntity
{
    public Guid TemplateId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal EstimatedUnitPrice { get; set; }
    public decimal TotalPrice => Quantity * EstimatedUnitPrice;

    // Navigation properties
    public QuoteTemplate Template { get; set; } = null!;
}
