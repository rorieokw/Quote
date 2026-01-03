using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.Common.Models;
using Quote.Application.Jobs.Commands.CreateJob;
using Quote.Application.Jobs.Queries.GetJob;
using Quote.Application.Jobs.Queries.GetJobs;
using Quote.Application.Jobs.Queries.GetJobWithTravelInfo;
using Quote.Application.Jobs.Queries.GetJobsClustered;
using Quote.Shared.DTOs;
using System.Security.Claims;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public JobsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedList<Application.Jobs.Queries.GetJobs.JobDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobs([FromQuery] GetJobsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJob(Guid id)
    {
        var result = await _mediator.Send(new GetJobQuery { JobId = id });

        if (!result.Succeeded)
        {
            return NotFound(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    [HttpGet("{id:guid}/travel")]
    [ProducesResponseType(typeof(JobWithTravelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobWithTravel(Guid id, [FromQuery] double lat, [FromQuery] double lng)
    {
        var result = await _mediator.Send(new GetJobWithTravelInfoQuery(id, lat, lng));

        if (!result.Succeeded)
        {
            return NotFound(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    [HttpGet("map")]
    [Authorize]
    [ProducesResponseType(typeof(JobMapResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobsForMap([FromQuery] double? lat, [FromQuery] double? lng, [FromQuery] double radius = 50)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var tradieId))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetJobsClusteredQuery(tradieId, lat, lng, radius));

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(CreateJobResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return CreatedAtAction(nameof(GetJob), new { id = result.Data!.JobId }, result.Data);
    }
}
