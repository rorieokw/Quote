using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.PriceBenchmarking.Queries.CompareQuotePrice;
using Quote.Application.PriceBenchmarking.Queries.GetPriceBenchmark;
using Quote.Application.PriceBenchmarking.Queries.GetTradieQuotesComparison;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/pricing")]
[Authorize]
public class PriceBenchmarkingController : ControllerBase
{
    private readonly IMediator _mediator;

    public PriceBenchmarkingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get price benchmark for a trade category and optional location
    /// </summary>
    [HttpGet("benchmarks/{tradeCategoryId}")]
    [ProducesResponseType(typeof(PriceBenchmarkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBenchmark(Guid tradeCategoryId, [FromQuery] string? postcode = null)
    {
        var query = new GetPriceBenchmarkQuery
        {
            TradeCategoryId = tradeCategoryId,
            Postcode = postcode
        };

        var result = await _mediator.Send(query);

        if (!result.Succeeded)
        {
            return NotFound(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Compare a specific quote to market benchmark
    /// </summary>
    [HttpGet("compare/{quoteId}")]
    [ProducesResponseType(typeof(QuotePriceComparisonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CompareQuote(Guid quoteId)
    {
        var query = new CompareQuotePriceQuery { QuoteId = quoteId };

        var result = await _mediator.Send(query);

        if (!result.Succeeded)
        {
            return NotFound(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get comparison of all tradie's quotes to market benchmarks
    /// </summary>
    [HttpGet("my-comparison")]
    [ProducesResponseType(typeof(TradieQuotesComparisonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyQuotesComparison()
    {
        var query = new GetTradieQuotesComparisonQuery();

        var result = await _mediator.Send(query);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }
}
