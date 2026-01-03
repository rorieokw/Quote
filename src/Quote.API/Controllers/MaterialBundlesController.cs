using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.MaterialBundles.Commands.CreateBundle;
using Quote.Application.MaterialBundles.Commands.DeleteBundle;
using Quote.Application.MaterialBundles.Commands.UpdateBundle;
using Quote.Application.MaterialBundles.Queries.GetBundleById;
using Quote.Application.MaterialBundles.Queries.GetBundles;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/material-bundles")]
[Authorize]
public class MaterialBundlesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MaterialBundlesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all material bundles for the current tradie
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(MaterialBundleListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBundles(
        [FromQuery] Guid? tradeCategoryId = null,
        [FromQuery] bool includeInactive = false)
    {
        var query = new GetBundlesQuery
        {
            TradeCategoryId = tradeCategoryId,
            IncludeInactive = includeInactive
        };

        var result = await _mediator.Send(query);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get a specific material bundle by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MaterialBundleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBundle(Guid id)
    {
        var query = new GetBundleByIdQuery { BundleId = id };

        var result = await _mediator.Send(query);

        if (!result.Succeeded)
        {
            return NotFound(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Create a new material bundle
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateMaterialBundleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateBundle([FromBody] CreateMaterialBundleRequest request)
    {
        var command = new CreateBundleCommand
        {
            Name = request.Name,
            Description = request.Description,
            TradeCategoryId = request.TradeCategoryId,
            Items = request.Items.Select(i => new CreateBundleItemCommand
            {
                ProductName = i.ProductName,
                SupplierName = i.SupplierName,
                ProductUrl = i.ProductUrl,
                DefaultQuantity = i.DefaultQuantity,
                Unit = i.Unit,
                EstimatedUnitPrice = i.EstimatedUnitPrice
            }).ToList()
        };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        var response = new CreateMaterialBundleResponse(result.Data!.BundleId, result.Data.Name);

        return CreatedAtAction(nameof(GetBundle), new { id = result.Data.BundleId }, response);
    }

    /// <summary>
    /// Update an existing material bundle
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateBundle(Guid id, [FromBody] UpdateMaterialBundleRequest request)
    {
        var command = new UpdateBundleCommand
        {
            BundleId = id,
            Name = request.Name,
            Description = request.Description,
            TradeCategoryId = request.TradeCategoryId,
            IsActive = request.IsActive,
            Items = request.Items.Select(i => new UpdateBundleItemCommand
            {
                Id = i.Id,
                ProductName = i.ProductName,
                SupplierName = i.SupplierName,
                ProductUrl = i.ProductUrl,
                DefaultQuantity = i.DefaultQuantity,
                Unit = i.Unit,
                EstimatedUnitPrice = i.EstimatedUnitPrice
            }).ToList()
        };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Contains("not found"))
                ? NotFound(new { errors = result.Errors })
                : BadRequest(new { errors = result.Errors });
        }

        return Ok(new { bundleId = result.Data!.BundleId, name = result.Data.Name });
    }

    /// <summary>
    /// Delete a material bundle
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteBundle(Guid id)
    {
        var command = new DeleteBundleCommand { BundleId = id };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return NotFound(new { errors = result.Errors });
        }

        return NoContent();
    }
}
