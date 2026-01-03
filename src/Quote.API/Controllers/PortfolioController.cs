using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfolioController : ControllerBase
{
    private readonly IMediator _mediator;

    public PortfolioController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get current tradie's portfolio
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PortfolioListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPortfolio()
    {
        // TODO: Implement GetPortfolioQuery
        return Ok(new PortfolioListResponse(new List<PortfolioItemDto>(), 0));
    }

    /// <summary>
    /// Get public portfolio for a tradie
    /// </summary>
    [HttpGet("/api/tradies/{tradieId}/portfolio")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TradiePortfolioDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicPortfolio(Guid tradieId)
    {
        // TODO: Implement GetPublicPortfolioQuery
        return Ok(new TradiePortfolioDto(tradieId, "Tradie Name", null, 4.5m, 50, new List<PortfolioItemDto>(), new List<PortfolioItemDto>()));
    }

    /// <summary>
    /// Create a new portfolio item
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePortfolioItem([FromBody] CreatePortfolioItemRequest request)
    {
        // TODO: Implement CreatePortfolioItemCommand
        var newId = Guid.NewGuid();
        return CreatedAtAction(nameof(GetPortfolio), new { id = newId }, new { id = newId });
    }

    /// <summary>
    /// Update a portfolio item
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePortfolioItem(Guid id, [FromBody] UpdatePortfolioItemRequest request)
    {
        // TODO: Implement UpdatePortfolioItemCommand
        return Ok(new { message = "Portfolio item updated" });
    }

    /// <summary>
    /// Delete a portfolio item
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePortfolioItem(Guid id)
    {
        // TODO: Implement DeletePortfolioItemCommand
        return NoContent();
    }

    /// <summary>
    /// Add media to a portfolio item
    /// </summary>
    [HttpPost("{id}/media")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> AddPortfolioMedia(Guid id, [FromBody] AddPortfolioMediaRequest request)
    {
        // TODO: Implement AddPortfolioMediaCommand
        return CreatedAtAction(nameof(GetPortfolio), null, new { message = "Media added" });
    }
}
