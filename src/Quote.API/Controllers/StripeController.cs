using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.Payments.Services;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/stripe")]
[Authorize]
public class StripeController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly IConfiguration _configuration;

    public StripeController(IStripeService stripeService, IConfiguration configuration)
    {
        _stripeService = stripeService;
        _configuration = configuration;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Create a Stripe Connected Account for a tradie
    /// </summary>
    [HttpPost("connect")]
    public async Task<ActionResult<ConnectedAccountResponse>> CreateConnectedAccount(
        [FromBody] CreateConnectedAccountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _stripeService.CreateConnectedAccountAsync(userId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get onboarding link for completing Stripe Connect setup
    /// </summary>
    [HttpGet("onboarding-link")]
    public async Task<ActionResult<OnboardingLinkResponse>> GetOnboardingLink(
        [FromQuery] string? returnUrl = null,
        [FromQuery] string? refreshUrl = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetCurrentUserId();
            var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:5000";
            var finalReturnUrl = returnUrl ?? $"{baseUrl}/tradie/stripe/return";
            var finalRefreshUrl = refreshUrl ?? $"{baseUrl}/tradie/stripe/refresh";

            var response = await _stripeService.GetOnboardingLinkAsync(userId, finalReturnUrl, finalRefreshUrl, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get the current Stripe account status
    /// </summary>
    [HttpGet("account-status")]
    public async Task<ActionResult<AccountStatusResponse>> GetAccountStatus(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var response = await _stripeService.GetAccountStatusAsync(userId, cancellationToken);

        if (response == null)
        {
            return NotFound("No Stripe account found. Please create one first.");
        }

        return Ok(response);
    }

    /// <summary>
    /// Refresh account status from Stripe
    /// </summary>
    [HttpPost("refresh-status")]
    public async Task<ActionResult> RefreshAccountStatus(
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _stripeService.RefreshAccountStatusAsync(userId, cancellationToken);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get balance for the connected account
    /// </summary>
    [HttpGet("balance")]
    public async Task<ActionResult<BalanceResponse>> GetBalance(
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _stripeService.GetBalanceAsync(userId, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
