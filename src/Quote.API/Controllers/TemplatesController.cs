using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.QuoteTemplates.Commands.CreateTemplate;
using Quote.Application.QuoteTemplates.Queries.GetTemplates;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public TemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all quote templates for the current tradie
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(TemplateListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTemplates(
        [FromQuery] Guid? tradeCategoryId = null,
        [FromQuery] bool includeInactive = false)
    {
        var query = new GetTemplatesQuery
        {
            TradeCategoryId = tradeCategoryId,
            IncludeInactive = includeInactive
        };

        var result = await _mediator.Send(query);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        var templates = result.Data!.Templates.Select(t => new QuoteTemplateDto(
            t.Id,
            t.Name,
            t.Description,
            t.TradeCategoryId,
            t.TradeCategoryName,
            t.DefaultLabourCost,
            t.DefaultMaterialsCost,
            t.DefaultDurationHours,
            t.DefaultNotes,
            t.UsageCount,
            t.IsActive,
            t.CreatedAt,
            t.Materials.Select(m => new Shared.DTOs.TemplateMaterialDto(
                m.Id,
                m.ProductName,
                m.Quantity,
                m.Unit,
                m.EstimatedUnitPrice,
                m.TotalPrice
            )).ToList()
        )).ToList();

        return Ok(new TemplateListResponse(templates, result.Data.TotalCount));
    }

    /// <summary>
    /// Create a new quote template
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(QuoteTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        var command = new CreateTemplateCommand
        {
            Name = request.Name,
            Description = request.Description,
            TradeCategoryId = request.TradeCategoryId,
            DefaultLabourCost = request.DefaultLabourCost,
            DefaultMaterialsCost = request.DefaultMaterialsCost,
            DefaultDurationHours = request.DefaultDurationHours,
            DefaultNotes = request.DefaultNotes,
            Materials = request.Materials?.Select(m => new CreateTemplateMaterialCommand
            {
                ProductName = m.ProductName,
                Quantity = m.Quantity,
                Unit = m.Unit,
                EstimatedUnitPrice = m.EstimatedUnitPrice
            }).ToList()
        };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return CreatedAtAction(
            nameof(GetTemplates),
            new { id = result.Data!.TemplateId },
            new { id = result.Data.TemplateId, name = result.Data.Name }
        );
    }

    /// <summary>
    /// Update an existing quote template
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] UpdateTemplateRequest request)
    {
        // TODO: Implement UpdateTemplateCommand
        return Ok(new { message = "Template updated", id });
    }

    /// <summary>
    /// Delete a quote template
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate(Guid id)
    {
        // TODO: Implement DeleteTemplateCommand
        return NoContent();
    }
}
