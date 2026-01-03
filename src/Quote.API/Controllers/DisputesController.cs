using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.Disputes.Commands.CloseDispute;
using Quote.Application.Disputes.Commands.CreateDispute;
using Quote.Application.Disputes.Commands.ResolveDispute;
using Quote.Application.Disputes.Queries.GetAllDisputes;
using Quote.Application.Disputes.Queries.GetDisputeById;
using Quote.Application.Disputes.Queries.GetMyDisputes;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/disputes")]
[Authorize]
public class DisputesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DisputesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all disputes (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(DisputeListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllDisputes(
        [FromQuery] string? status = null,
        [FromQuery] string? reason = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetAllDisputesQuery
        {
            Status = status,
            Reason = reason,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get current user's disputes
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(DisputeListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyDisputes(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetMyDisputesQuery
        {
            Status = status,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get a specific dispute by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DisputeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDispute(Guid id)
    {
        var query = new GetDisputeByIdQuery { DisputeId = id };

        var result = await _mediator.Send(query);

        if (!result.Succeeded)
        {
            return NotFound(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Create a new dispute
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Quote.Shared.DTOs.CreateDisputeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateDispute([FromBody] CreateDisputeRequest request)
    {
        var command = new CreateDisputeCommand
        {
            JobQuoteId = request.JobQuoteId,
            Reason = request.Reason,
            Description = request.Description
        };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        var response = new Quote.Shared.DTOs.CreateDisputeResponse(result.Data!.DisputeId);

        return CreatedAtAction(nameof(GetDispute), new { id = result.Data.DisputeId }, response);
    }

    /// <summary>
    /// Resolve a dispute (Admin only)
    /// </summary>
    [HttpPut("{id}/resolve")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResolveDispute(Guid id, [FromBody] ResolveDisputeRequest request)
    {
        var command = new ResolveDisputeCommand
        {
            DisputeId = id,
            ResolutionType = request.ResolutionType,
            Resolution = request.Resolution,
            RefundAmount = request.RefundAmount,
            AdminNotes = request.AdminNotes
        };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
            {
                return NotFound(new { errors = result.Errors });
            }
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new { message = "Dispute resolved successfully" });
    }

    /// <summary>
    /// Close/withdraw a dispute
    /// </summary>
    [HttpPut("{id}/close")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CloseDispute(Guid id, [FromBody] CloseDisputeRequest? request = null)
    {
        var command = new CloseDisputeCommand
        {
            DisputeId = id,
            Reason = request?.Reason
        };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
            {
                return NotFound(new { errors = result.Errors });
            }
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new { message = "Dispute closed successfully" });
    }
}
