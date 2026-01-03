using Quote.Domain.Common;
using Quote.Domain.Enums;

namespace Quote.Domain.Entities;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid TradieId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? QuoteId { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }

    // Amounts
    public decimal Subtotal { get; set; }
    public decimal GstAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue => TotalAmount - AmountPaid;

    // Tradie/Business details (captured at invoice time)
    public string TradieBusinessName { get; set; } = string.Empty;
    public string? TradieAbn { get; set; }
    public string TradieEmail { get; set; } = string.Empty;
    public string? TradiePhone { get; set; }
    public string? TradieAddress { get; set; }

    // Customer details (captured at invoice time)
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerAddress { get; set; }

    // Additional info
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? TermsAndConditions { get; set; }

    // Tracking
    public DateTime? SentAt { get; set; }
    public DateTime? ViewedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public int ViewCount { get; set; }

    // Payment options
    public bool AcceptOnlinePayment { get; set; } = true;
    public bool AcceptBankTransfer { get; set; } = true;
    public string? BankAccountName { get; set; }
    public string? BankBsb { get; set; }
    public string? BankAccountNumber { get; set; }

    // Navigation properties
    public User Tradie { get; set; } = null!;
    public User Customer { get; set; } = null!;
    public Job? Job { get; set; }
    public JobQuote? Quote { get; set; }
    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
    public ICollection<InvoicePayment> Payments { get; set; } = new List<InvoicePayment>();
}
