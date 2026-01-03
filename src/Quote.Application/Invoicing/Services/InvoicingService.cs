using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Domain.Entities;
using Quote.Domain.Enums;
using Quote.Shared.DTOs;

namespace Quote.Application.Invoicing.Services;

public class InvoicingService : IInvoicingService
{
    private readonly IApplicationDbContext _context;
    private const decimal GstRate = 0.10m;

    public InvoicingService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Invoice> CreateInvoiceAsync(Guid tradieId, CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var tradie = await _context.Users
            .Include(u => u.TradieProfile)
            .FirstOrDefaultAsync(u => u.Id == tradieId, cancellationToken)
            ?? throw new InvalidOperationException("Tradie not found");

        var customer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer not found");

        Job? job = null;
        if (request.JobId.HasValue)
        {
            job = await _context.Jobs.FirstOrDefaultAsync(j => j.Id == request.JobId.Value, cancellationToken);
        }

        var lineItems = request.LineItems.Select((li, index) => new InvoiceLineItem
        {
            Id = Guid.NewGuid(),
            Order = index + 1,
            Description = li.Description,
            Quantity = li.Quantity,
            Unit = li.Unit,
            UnitPrice = li.UnitPrice,
            IsTaxable = li.IsTaxable
        }).ToList();

        var subtotal = lineItems.Sum(li => li.Amount);
        var taxableAmount = lineItems.Where(li => li.IsTaxable).Sum(li => li.Amount);
        var gstAmount = Math.Round(taxableAmount * GstRate, 2);
        var totalAmount = subtotal + gstAmount;

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = GenerateInvoiceNumber(tradieId),
            TradieId = tradieId,
            CustomerId = customer.Id,
            JobId = request.JobId,
            QuoteId = request.QuoteId,
            Status = InvoiceStatus.Draft,
            InvoiceDate = DateTime.UtcNow,
            DueDate = request.DueDate ?? DateTime.UtcNow.AddDays(14),
            Subtotal = subtotal,
            GstAmount = gstAmount,
            TotalAmount = totalAmount,
            AmountPaid = 0,
            TradieBusinessName = tradie.TradieProfile?.BusinessName ?? $"{tradie.FirstName} {tradie.LastName}",
            TradieAbn = null, // ABN can be added to TradieProfile in future
            TradieEmail = tradie.Email,
            TradiePhone = tradie.Phone,
            CustomerName = $"{customer.FirstName} {customer.LastName}",
            CustomerEmail = customer.Email,
            CustomerPhone = customer.Phone,
            Description = request.Description,
            Notes = request.Notes,
            TermsAndConditions = request.TermsAndConditions,
            AcceptOnlinePayment = request.AcceptOnlinePayment,
            AcceptBankTransfer = request.AcceptBankTransfer
        };

        foreach (var lineItem in lineItems)
        {
            lineItem.InvoiceId = invoice.Id;
            invoice.LineItems.Add(lineItem);
        }

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        return invoice;
    }

    public async Task<Invoice> CreateInvoiceFromQuoteAsync(Guid tradieId, CreateInvoiceFromQuoteRequest request, CancellationToken cancellationToken = default)
    {
        var quote = await _context.Quotes
            .Include(q => q.Job)
            .Include(q => q.Materials)
            .Include(q => q.Tradie)
            .ThenInclude(t => t.TradieProfile)
            .FirstOrDefaultAsync(q => q.Id == request.QuoteId && q.TradieId == tradieId, cancellationToken)
            ?? throw new InvalidOperationException("Quote not found");

        var customer = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == quote.Job.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer not found");

        var lineItems = new List<CreateInvoiceLineItemRequest>();

        // Add labour as first line item
        if (quote.LabourCost > 0)
        {
            lineItems.Add(new CreateInvoiceLineItemRequest(
                Description: $"Labour - {quote.Job.Title}",
                Quantity: quote.EstimatedDurationHours,
                Unit: "hours",
                UnitPrice: Math.Round(quote.LabourCost / quote.EstimatedDurationHours, 2),
                IsTaxable: true
            ));
        }

        // Add materials
        foreach (var material in quote.Materials)
        {
            lineItems.Add(new CreateInvoiceLineItemRequest(
                Description: material.ProductName,
                Quantity: material.Quantity,
                Unit: material.Unit,
                UnitPrice: material.UnitPrice,
                IsTaxable: true
            ));
        }

        // If no materials were added but there's a materials cost, add as single line
        if (!quote.Materials.Any() && quote.MaterialsCost > 0)
        {
            lineItems.Add(new CreateInvoiceLineItemRequest(
                Description: "Materials",
                Quantity: 1,
                Unit: null,
                UnitPrice: quote.MaterialsCost,
                IsTaxable: true
            ));
        }

        var createRequest = new CreateInvoiceRequest(
            JobId: quote.JobId,
            QuoteId: quote.Id,
            CustomerId: customer.Id,
            DueDate: request.DueDate,
            LineItems: lineItems,
            Description: $"Invoice for {quote.Job.Title}",
            Notes: request.AdditionalNotes ?? quote.Notes,
            TermsAndConditions: null,
            AcceptOnlinePayment: true,
            AcceptBankTransfer: true
        );

        return await CreateInvoiceAsync(tradieId, createRequest, cancellationToken);
    }

    public async Task<InvoiceDto?> GetInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .Include(i => i.LineItems.OrderBy(li => li.Order))
            .Include(i => i.Payments)
            .Include(i => i.Job)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (invoice == null)
            return null;

        return MapToDto(invoice);
    }

    public async Task<InvoicesResponse> GetInvoicesAsync(Guid tradieId, int page, int pageSize, string? status, CancellationToken cancellationToken = default)
    {
        var query = _context.Invoices
            .Include(i => i.Job)
            .Where(i => i.TradieId == tradieId);

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<InvoiceStatus>(status, true, out var statusEnum))
        {
            query = query.Where(i => i.Status == statusEnum);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var allInvoices = await _context.Invoices
            .Where(i => i.TradieId == tradieId)
            .ToListAsync(cancellationToken);

        var stats = new InvoiceStats(
            TotalInvoices: allInvoices.Count,
            DraftCount: allInvoices.Count(i => i.Status == InvoiceStatus.Draft),
            SentCount: allInvoices.Count(i => i.Status == InvoiceStatus.Sent),
            OverdueCount: allInvoices.Count(i => i.Status == InvoiceStatus.Overdue || (i.Status == InvoiceStatus.Sent && i.DueDate < now)),
            PaidCount: allInvoices.Count(i => i.Status == InvoiceStatus.Paid),
            TotalOutstanding: allInvoices.Where(i => i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled).Sum(i => i.AmountDue),
            TotalPaidThisMonth: allInvoices.Where(i => i.PaidAt >= monthStart).Sum(i => i.AmountPaid),
            TotalInvoicedThisMonth: allInvoices.Where(i => i.InvoiceDate >= monthStart).Sum(i => i.TotalAmount)
        );

        return new InvoicesResponse(
            Invoices: invoices.Select(i => new InvoiceListItemDto(
                Id: i.Id,
                InvoiceNumber: i.InvoiceNumber,
                Status: i.Status.ToString(),
                InvoiceDate: i.InvoiceDate,
                DueDate: i.DueDate,
                TotalAmount: i.TotalAmount,
                AmountDue: i.AmountDue,
                CustomerName: i.CustomerName,
                JobTitle: i.Job?.Title,
                IsOverdue: i.Status != InvoiceStatus.Paid && i.Status != InvoiceStatus.Cancelled && i.DueDate < now
            )).ToList(),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize,
            Stats: stats
        );
    }

    public async Task<Invoice?> SendInvoiceAsync(Guid tradieId, SendInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && i.TradieId == tradieId, cancellationToken);

        if (invoice == null)
            return null;

        invoice.Status = InvoiceStatus.Sent;
        invoice.SentAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // TODO: Send email notification to customer

        return invoice;
    }

    public async Task<Invoice?> RecordPaymentAsync(Guid tradieId, RecordPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && i.TradieId == tradieId, cancellationToken);

        if (invoice == null)
            return null;

        var payment = new InvoicePayment
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            PaymentDate = request.PaymentDate ?? DateTime.UtcNow,
            Reference = request.Reference,
            Notes = request.Notes,
            IsConfirmed = true
        };

        invoice.Payments.Add(payment);
        invoice.AmountPaid += request.Amount;

        if (invoice.AmountPaid >= invoice.TotalAmount)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
        }
        else if (invoice.AmountPaid > 0)
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return invoice;
    }

    public async Task<byte[]> GeneratePdfAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .Include(i => i.LineItems.OrderBy(li => li.Order))
            .Include(i => i.Payments)
            .Include(i => i.Job)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (invoice == null)
            throw new InvalidOperationException("Invoice not found");

        // Generate PDF using QuestPDF (simplified version)
        return GenerateInvoicePdf(invoice);
    }

    public async Task MarkAsViewedAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (invoice == null)
            return;

        if (invoice.ViewedAt == null)
        {
            invoice.ViewedAt = DateTime.UtcNow;
            if (invoice.Status == InvoiceStatus.Sent)
            {
                invoice.Status = InvoiceStatus.Viewed;
            }
        }

        invoice.ViewCount++;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CancelInvoiceAsync(Guid tradieId, Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TradieId == tradieId, cancellationToken);

        if (invoice == null)
            return false;

        if (invoice.Status == InvoiceStatus.Paid || invoice.AmountPaid > 0)
        {
            throw new InvalidOperationException("Cannot cancel an invoice that has payments");
        }

        invoice.Status = InvoiceStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public string GenerateInvoiceNumber(Guid tradieId)
    {
        var prefix = "INV";
        var datePart = DateTime.UtcNow.ToString("yyyyMM");
        var random = new Random().Next(1000, 9999);
        return $"{prefix}-{datePart}-{random}";
    }

    private InvoiceDto MapToDto(Invoice invoice)
    {
        return new InvoiceDto(
            Id: invoice.Id,
            InvoiceNumber: invoice.InvoiceNumber,
            Status: invoice.Status.ToString(),
            InvoiceDate: invoice.InvoiceDate,
            DueDate: invoice.DueDate,
            Subtotal: invoice.Subtotal,
            GstAmount: invoice.GstAmount,
            TotalAmount: invoice.TotalAmount,
            AmountPaid: invoice.AmountPaid,
            AmountDue: invoice.AmountDue,
            TradieBusinessName: invoice.TradieBusinessName,
            CustomerName: invoice.CustomerName,
            CustomerEmail: invoice.CustomerEmail,
            JobId: invoice.JobId,
            JobTitle: invoice.Job?.Title,
            ViewCount: invoice.ViewCount,
            SentAt: invoice.SentAt,
            ViewedAt: invoice.ViewedAt,
            PaidAt: invoice.PaidAt,
            LineItems: invoice.LineItems.Select(li => new InvoiceLineItemDto(
                Id: li.Id,
                Order: li.Order,
                Description: li.Description,
                Quantity: li.Quantity,
                Unit: li.Unit,
                UnitPrice: li.UnitPrice,
                Amount: li.Amount,
                IsTaxable: li.IsTaxable
            )).ToList(),
            Payments: invoice.Payments.Select(p => new InvoicePaymentDto(
                Id: p.Id,
                Amount: p.Amount,
                PaymentMethod: p.PaymentMethod,
                PaymentDate: p.PaymentDate,
                TransactionId: p.TransactionId,
                Reference: p.Reference,
                Notes: p.Notes,
                IsConfirmed: p.IsConfirmed
            )).ToList()
        );
    }

    private byte[] GenerateInvoicePdf(Invoice invoice)
    {
        // Simple PDF generation - in production, use QuestPDF properly
        // For now, return a placeholder that could be replaced with actual PDF generation
        using var stream = new MemoryStream();

        // Create a simple text-based representation
        // In production, replace with QuestPDF document generation
        using var writer = new StreamWriter(stream);
        writer.WriteLine($"INVOICE #{invoice.InvoiceNumber}");
        writer.WriteLine($"Date: {invoice.InvoiceDate:d}");
        writer.WriteLine($"Due: {invoice.DueDate:d}");
        writer.WriteLine();
        writer.WriteLine($"From: {invoice.TradieBusinessName}");
        writer.WriteLine($"To: {invoice.CustomerName}");
        writer.WriteLine();
        writer.WriteLine("Line Items:");
        foreach (var item in invoice.LineItems)
        {
            writer.WriteLine($"  {item.Description}: {item.Quantity} x ${item.UnitPrice:N2} = ${item.Amount:N2}");
        }
        writer.WriteLine();
        writer.WriteLine($"Subtotal: ${invoice.Subtotal:N2}");
        writer.WriteLine($"GST (10%): ${invoice.GstAmount:N2}");
        writer.WriteLine($"Total: ${invoice.TotalAmount:N2}");
        writer.Flush();

        return stream.ToArray();
    }
}
