using Quote.Domain.Entities;
using Quote.Shared.DTOs;

namespace Quote.Application.Invoicing.Services;

public interface IInvoicingService
{
    Task<Invoice> CreateInvoiceAsync(Guid tradieId, CreateInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<Invoice> CreateInvoiceFromQuoteAsync(Guid tradieId, CreateInvoiceFromQuoteRequest request, CancellationToken cancellationToken = default);
    Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<InvoicesResponse> GetInvoicesAsync(Guid tradieId, int page, int pageSize, string? status, CancellationToken cancellationToken = default);
    Task<Invoice?> SendInvoiceAsync(Guid tradieId, SendInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<Invoice?> RecordPaymentAsync(Guid tradieId, RecordPaymentRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> GeneratePdfAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task MarkAsViewedAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<bool> CancelInvoiceAsync(Guid tradieId, Guid invoiceId, CancellationToken cancellationToken = default);
    string GenerateInvoiceNumber(Guid tradieId);
}
