using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class InvoicePayment : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // "stripe", "bank_transfer", "cash", "cheque"
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public string? TransactionId { get; set; } // Stripe payment intent ID or reference
    public string? Reference { get; set; } // Bank transfer reference or cheque number
    public string? Notes { get; set; }
    public bool IsConfirmed { get; set; }

    // Navigation
    public Invoice Invoice { get; set; } = null!;
}
