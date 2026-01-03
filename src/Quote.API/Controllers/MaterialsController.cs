using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MaterialsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MaterialsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get available material estimate templates for a trade category
    /// </summary>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(List<MaterialEstimateTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplates([FromQuery] Guid? tradeCategoryId = null)
    {
        // TODO: Implement GetMaterialTemplatesQuery
        return Ok(new List<MaterialEstimateTemplateDto>());
    }

    /// <summary>
    /// Calculate materials estimate based on template and inputs
    /// </summary>
    [HttpPost("estimate")]
    [ProducesResponseType(typeof(MaterialEstimateResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CalculateEstimate([FromBody] CalculateMaterialsRequest request)
    {
        // TODO: Implement CalculateMaterialsQuery
        return Ok(new MaterialEstimateResultDto(
            request.TemplateId,
            "Standard Install",
            new List<MaterialLineItemDto>(),
            0
        ));
    }

    /// <summary>
    /// Create a custom material estimate template
    /// </summary>
    [HttpPost("templates")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateMaterialTemplateRequest request)
    {
        // TODO: Implement CreateMaterialTemplateCommand
        var newId = Guid.NewGuid();
        return CreatedAtAction(nameof(GetTemplates), new { id = newId }, new { id = newId });
    }
}
