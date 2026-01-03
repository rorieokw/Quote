using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.LeadScoring.Commands.RecalculateScores;
using Quote.Application.LeadScoring.Queries.GetJobScore;
using Quote.Application.LeadScoring.Queries.GetScoredLeads;
using Quote.Shared.DTOs;
using System.Security.Claims;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeadsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LeadsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get scored leads for the current tradie, ordered by relevance
    /// </summary>
    [HttpGet("scored")]
    [ProducesResponseType(typeof(ScoredLeadsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetScoredLeads(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? minScore = null,
        [FromQuery] string? rating = null,
        [FromQuery] Guid? tradeCategoryId = null,
        [FromQuery] bool refresh = false)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var tradieId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetScoredLeadsQuery
        {
            TradieId = tradieId,
            PageNumber = page,
            PageSize = pageSize,
            MinScore = minScore,
            ScoreRating = rating,
            TradeCategoryId = tradeCategoryId,
            RefreshScores = refresh
        });

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get the lead score for a specific job
    /// </summary>
    [HttpGet("{jobId:guid}/score")]
    [ProducesResponseType(typeof(LeadScoreDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetJobScore(Guid jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var tradieId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetJobScoreQuery(tradieId, jobId));

        if (!result.Succeeded)
        {
            return NotFound(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Trigger recalculation of all lead scores for the current tradie
    /// </summary>
    [HttpPost("recalculate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RecalculateScores()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var tradieId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new RecalculateScoresCommand(tradieId));

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new { message = "Scores recalculated successfully" });
    }
}
