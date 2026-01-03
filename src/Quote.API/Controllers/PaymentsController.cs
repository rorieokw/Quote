using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.Payments.Services;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IStripeService _stripeService;

    public PaymentsController(IStripeService stripeService)
    {
        _stripeService = stripeService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Create a payment intent for an invoice, quote, or milestone
    /// </summary>
    [HttpPost("create-intent")]
    public async Task<ActionResult<PaymentIntentResponse>> CreatePaymentIntent(
        [FromBody] CreatePaymentIntentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _stripeService.CreatePaymentIntentAsync(userId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Confirm a payment after client-side payment completion
    /// </summary>
    [HttpPost("confirm")]
    public async Task<ActionResult<ConfirmPaymentResponse>> ConfirmPayment(
        [FromBody] ConfirmPaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _stripeService.ConfirmPaymentAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return BadRequest(new ConfirmPaymentResponse(false, "error", null, ex.Message));
        }
    }

    /// <summary>
    /// Process a deposit payment for an accepted quote
    /// </summary>
    [HttpPost("deposits/{quoteId}")]
    public async Task<ActionResult<DepositResponse>> ProcessDeposit(
        Guid quoteId,
        [FromBody] ProcessDepositRequest request,
        CancellationToken cancellationToken)
    {
        if (quoteId != request.QuoteId)
        {
            return BadRequest("Quote ID mismatch");
        }

        try
        {
            var userId = GetCurrentUserId();
            var response = await _stripeService.ProcessDepositAsync(userId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Process a milestone payment
    /// </summary>
    [HttpPost("milestones/{milestoneId}")]
    public async Task<ActionResult<MilestonePaymentResponse>> ProcessMilestonePayment(
        Guid milestoneId,
        [FromBody] ProcessMilestonePaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (milestoneId != request.MilestoneId)
        {
            return BadRequest("Milestone ID mismatch");
        }

        try
        {
            var userId = GetCurrentUserId();
            var response = await _stripeService.ProcessMilestonePaymentAsync(userId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get payment history for the current user
    /// </summary>
    [HttpGet("history")]
    public async Task<ActionResult<PaymentHistoryResponse>> GetPaymentHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool isTradie = false,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var response = await _stripeService.GetPaymentHistoryAsync(userId, isTradie, page, pageSize, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Request a refund for a payment
    /// </summary>
    [HttpPost("refunds")]
    public async Task<ActionResult<RefundResponse>> RefundPayment(
        [FromBody] RefundRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _stripeService.RefundPaymentAsync(userId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new RefundResponse(false, "", 0, "error", ex.Message));
        }
    }
}
