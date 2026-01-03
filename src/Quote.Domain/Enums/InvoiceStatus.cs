namespace Quote.Domain.Enums;

public enum InvoiceStatus
{
    Draft,
    Sent,
    Viewed,
    PartiallyPaid,
    Paid,
    Overdue,
    Cancelled,
    Refunded
}
