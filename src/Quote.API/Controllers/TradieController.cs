using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TradieController : ControllerBase
{
    private readonly IMediator _mediator;

    public TradieController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get tradie's job preferences (min/max size, radius, blocked suburbs)
    /// </summary>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(JobPreferencesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreferences()
    {
        // TODO: Implement GetJobPreferencesQuery
        return Ok(new JobPreferencesDto(null, null, 25, new List<BlockedSuburbDto>()));
    }

    /// <summary>
    /// Update tradie's job preferences
    /// </summary>
    [HttpPut("preferences")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdateJobPreferencesRequest request)
    {
        // TODO: Implement UpdateJobPreferencesCommand
        return Ok(new { message = "Preferences updated" });
    }

    /// <summary>
    /// Get blocked suburbs
    /// </summary>
    [HttpGet("blocked-suburbs")]
    [ProducesResponseType(typeof(List<BlockedSuburbDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBlockedSuburbs()
    {
        // TODO: Implement GetBlockedSuburbsQuery
        return Ok(new List<BlockedSuburbDto>());
    }

    /// <summary>
    /// Block a suburb
    /// </summary>
    [HttpPost("blocked-suburbs")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> BlockSuburb([FromBody] BlockSuburbRequest request)
    {
        // TODO: Implement BlockSuburbCommand
        return CreatedAtAction(nameof(GetBlockedSuburbs), null, new { message = "Suburb blocked" });
    }

    /// <summary>
    /// Unblock a suburb
    /// </summary>
    [HttpDelete("blocked-suburbs/{postcode}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnblockSuburb(string postcode)
    {
        // TODO: Implement UnblockSuburbCommand
        return NoContent();
    }

    /// <summary>
    /// Get available now status
    /// </summary>
    [HttpGet("available-now")]
    [ProducesResponseType(typeof(AvailableNowDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableNow()
    {
        // TODO: Implement GetAvailableNowQuery
        return Ok(new AvailableNowDto(false, null));
    }

    /// <summary>
    /// Toggle available now status
    /// </summary>
    [HttpPut("available-now")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetAvailableNow([FromBody] SetAvailableNowRequest request)
    {
        // TODO: Implement SetAvailableNowCommand
        return Ok(new AvailableNowDto(request.IsAvailable, request.IsAvailable ? DateTime.UtcNow.AddHours(request.HoursAvailable ?? 8) : null));
    }

    /// <summary>
    /// Get verification status and badges
    /// </summary>
    [HttpGet("verification-status")]
    [ProducesResponseType(typeof(VerificationStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVerificationStatus()
    {
        // TODO: Implement GetVerificationStatusQuery
        return Ok(new VerificationStatusDto(true, true, false, true, "Verified", new List<string> { "Licensed", "Insured" }));
    }
}
