using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.Payments.Services;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PayoutsController : ControllerBase
{
    private readonly IStripeService _stripeService;

    public PayoutsController(IStripeService stripeService)
    {
        _stripeService = stripeService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Get payout history for the tradie
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PayoutListResponse>> GetPayouts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var response = await _stripeService.GetPayoutsAsync(userId, page, pageSize, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Request a payout to the tradie's bank account
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RequestPayoutResponse>> RequestPayout(
        [FromBody] RequestPayoutRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _stripeService.RequestPayoutAsync(userId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
