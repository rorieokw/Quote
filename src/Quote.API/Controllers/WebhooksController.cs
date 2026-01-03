using Microsoft.AspNetCore.Mvc;
using Quote.Application.Payments.Services;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IStripeService _stripeService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IStripeService stripeService, ILogger<WebhooksController> logger)
    {
        _stripeService = stripeService;
        _logger = logger;
    }

    /// <summary>
    /// Handle Stripe webhook events
    /// </summary>
    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook(CancellationToken cancellationToken)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("Stripe webhook received without signature");
            return BadRequest("Missing Stripe signature");
        }

        try
        {
            await _stripeService.HandleWebhookEventAsync(json, signature, cancellationToken);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return BadRequest("Webhook processing failed");
        }
    }
}
