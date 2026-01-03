using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.Invoicing.Services;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly IInvoicingService _invoicingService;

    public InvoicesController(IInvoicingService invoicingService)
    {
        _invoicingService = invoicingService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<InvoicesResponse>> GetInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var response = await _invoicingService.GetInvoicesAsync(userId, page, pageSize, status, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(
        Guid id,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoicingService.GetInvoiceAsync(id, cancellationToken);

        if (invoice == null)
        {
            return NotFound();
        }

        return Ok(invoice);
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> CreateInvoice(
        [FromBody] CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var invoice = await _invoicingService.CreateInvoiceAsync(userId, request, cancellationToken);
            var dto = await _invoicingService.GetInvoiceAsync(invoice.Id, cancellationToken);
            return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("from-quote")]
    public async Task<ActionResult<InvoiceDto>> CreateInvoiceFromQuote(
        [FromBody] CreateInvoiceFromQuoteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var invoice = await _invoicingService.CreateInvoiceFromQuoteAsync(userId, request, cancellationToken);
            var dto = await _invoicingService.GetInvoiceAsync(invoice.Id, cancellationToken);
            return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/send")]
    public async Task<ActionResult<InvoiceDto>> SendInvoice(
        Guid id,
        [FromBody] SendInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (id != request.InvoiceId)
        {
            return BadRequest("Invoice ID mismatch");
        }

        var userId = GetCurrentUserId();
        var invoice = await _invoicingService.SendInvoiceAsync(userId, request, cancellationToken);

        if (invoice == null)
        {
            return NotFound();
        }

        var dto = await _invoicingService.GetInvoiceAsync(invoice.Id, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id}/payments")]
    public async Task<ActionResult<InvoiceDto>> RecordPayment(
        Guid id,
        [FromBody] RecordPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (id != request.InvoiceId)
        {
            return BadRequest("Invoice ID mismatch");
        }

        var userId = GetCurrentUserId();
        var invoice = await _invoicingService.RecordPaymentAsync(userId, request, cancellationToken);

        if (invoice == null)
        {
            return NotFound();
        }

        var dto = await _invoicingService.GetInvoiceAsync(invoice.Id, cancellationToken);
        return Ok(dto);
    }

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GetPdf(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var pdfBytes = await _invoicingService.GeneratePdfAsync(id, cancellationToken);

            // Mark as viewed
            await _invoicingService.MarkAsViewedAsync(id, cancellationToken);

            return File(pdfBytes, "application/pdf", $"invoice-{id}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("{id}/view")]
    [AllowAnonymous]
    public async Task<ActionResult> MarkAsViewed(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _invoicingService.MarkAsViewedAsync(id, cancellationToken);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> CancelInvoice(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _invoicingService.CancelInvoiceAsync(userId, id, cancellationToken);

            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
