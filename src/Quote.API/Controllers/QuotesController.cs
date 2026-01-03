using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.Quotes.Commands.CreateQuickQuote;
using Quote.Application.Quotes.Queries.GetQuotesForComparison;
using Quote.Application.Quotes.Queries.GetTradieQuotes;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuotesController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuotesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Submit a quick quote for a job (tradie only)
    /// </summary>
    [HttpPost("quick")]
    [ProducesResponseType(typeof(QuickQuoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateQuickQuote([FromBody] QuickQuoteRequest request)
    {
        var command = new CreateQuickQuoteCommand
        {
            JobId = request.JobId,
            LabourCost = request.LabourCost,
            MaterialsCost = request.MaterialsCost,
            EstimatedDurationHours = request.EstimatedDurationHours,
            Notes = request.Notes,
            TemplateId = request.TemplateId,
            ProposedStartDate = request.ProposedStartDate,
            DepositRequired = request.DepositRequired,
            DepositPercentage = request.DepositPercentage,
            Materials = request.Materials,
            MaterialBundleId = request.MaterialBundleId,
            SaveMaterialsAsBundle = request.SaveMaterialsAsBundle,
            NewBundleName = request.NewBundleName
        };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new QuickQuoteResponse(
            result.Data!.QuoteId,
            result.Data.JobId,
            result.Data.TotalCost,
            result.Data.Status,
            result.Data.ValidUntil
        ));
    }

    /// <summary>
    /// Get all quotes submitted by the current tradie with status tracking
    /// </summary>
    [HttpGet("my-quotes")]
    [ProducesResponseType(typeof(QuoteListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyQuotes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        var query = new GetTradieQuotesQuery
        {
            Page = page,
            PageSize = pageSize,
            Status = status
        };

        var result = await _mediator.Send(query);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        var quotes = result.Data!.Quotes.Select(q => new QuoteStatusDto(
            q.QuoteId,
            q.JobId,
            q.JobTitle,
            q.JobSuburb,
            q.CustomerName,
            q.Status,
            q.TotalCost,
            q.ViewCount,
            q.FirstViewedAt,
            q.LastViewedAt,
            q.CompetingQuotes,
            q.IsWinningQuote,
            q.IsJobStillOpen,
            q.CreatedAt,
            q.ValidUntil
        )).ToList();

        return Ok(new QuoteListResponse(quotes, result.Data.TotalCount, result.Data.Page, result.Data.PageSize));
    }

    /// <summary>
    /// Track when a customer views a quote
    /// </summary>
    [HttpPost("{quoteId}/view")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TrackQuoteView(Guid quoteId)
    {
        // TODO: Implement TrackQuoteViewCommand
        return Ok(new { message = "View tracked" });
    }

    /// <summary>
    /// Get all quotes for a job to compare side-by-side (customer only)
    /// </summary>
    [HttpGet("compare/{jobId}")]
    [ProducesResponseType(typeof(QuoteComparisonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareQuotes(
        Guid jobId,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        var query = new GetQuotesForComparisonQuery
        {
            JobId = jobId,
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await _mediator.Send(query);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }
}
