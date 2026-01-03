namespace Quote.Shared.DTOs;

public record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    string Status,
    DateTime InvoiceDate,
    DateTime DueDate,
    decimal Subtotal,
    decimal GstAmount,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal AmountDue,
    string TradieBusinessName,
    string CustomerName,
    string CustomerEmail,
    Guid? JobId,
    string? JobTitle,
    int ViewCount,
    DateTime? SentAt,
    DateTime? ViewedAt,
    DateTime? PaidAt,
    List<InvoiceLineItemDto> LineItems,
    List<InvoicePaymentDto> Payments
);

public record InvoiceLineItemDto(
    Guid Id,
    int Order,
    string Description,
    decimal Quantity,
    string? Unit,
    decimal UnitPrice,
    decimal Amount,
    bool IsTaxable
);

public record InvoicePaymentDto(
    Guid Id,
    decimal Amount,
    string PaymentMethod,
    DateTime PaymentDate,
    string? TransactionId,
    string? Reference,
    string? Notes,
    bool IsConfirmed
);

public record CreateInvoiceRequest(
    Guid? JobId,
    Guid? QuoteId,
    Guid CustomerId,
    DateTime? DueDate,
    List<CreateInvoiceLineItemRequest> LineItems,
    string? Description,
    string? Notes,
    string? TermsAndConditions,
    bool AcceptOnlinePayment,
    bool AcceptBankTransfer
);

public record CreateInvoiceLineItemRequest(
    string Description,
    decimal Quantity,
    string? Unit,
    decimal UnitPrice,
    bool IsTaxable
);

public record CreateInvoiceFromQuoteRequest(
    Guid QuoteId,
    DateTime? DueDate,
    string? AdditionalNotes
);

public record SendInvoiceRequest(
    Guid InvoiceId,
    string? CustomMessage
);

public record RecordPaymentRequest(
    Guid InvoiceId,
    decimal Amount,
    string PaymentMethod,
    DateTime? PaymentDate,
    string? Reference,
    string? Notes
);

public record InvoiceListItemDto(
    Guid Id,
    string InvoiceNumber,
    string Status,
    DateTime InvoiceDate,
    DateTime DueDate,
    decimal TotalAmount,
    decimal AmountDue,
    string CustomerName,
    string? JobTitle,
    bool IsOverdue
);

public record InvoicesResponse(
    List<InvoiceListItemDto> Invoices,
    int TotalCount,
    int Page,
    int PageSize,
    InvoiceStats Stats
);

public record InvoiceStats(
    int TotalInvoices,
    int DraftCount,
    int SentCount,
    int OverdueCount,
    int PaidCount,
    decimal TotalOutstanding,
    decimal TotalPaidThisMonth,
    decimal TotalInvoicedThisMonth
);

public record InvoicePreviewDto(
    Guid InvoiceId,
    string InvoiceNumber,
    byte[] PdfBytes,
    string FileName
);
