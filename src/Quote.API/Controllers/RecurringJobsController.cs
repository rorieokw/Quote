using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.RecurringJobs.Services;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/recurring-jobs")]
[Authorize]
public class RecurringJobsController : ControllerBase
{
    private readonly IRecurringJobService _recurringJobService;

    public RecurringJobsController(IRecurringJobService recurringJobService)
    {
        _recurringJobService = recurringJobService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Get all recurring job templates for the current user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<RecurringJobTemplatesResponse>> GetTemplates(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? activeOnly = null,
        [FromQuery] bool isTradie = false,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var response = await _recurringJobService.GetTemplatesAsync(userId, isTradie, page, pageSize, activeOnly, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Get a specific recurring job template
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<RecurringJobTemplateDto>> GetTemplate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var template = await _recurringJobService.GetTemplateAsync(id, cancellationToken);

        if (template == null)
        {
            return NotFound();
        }

        return Ok(template);
    }

    /// <summary>
    /// Create a new recurring job template
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<RecurringJobTemplateDto>> CreateTemplate(
        [FromBody] CreateRecurringJobTemplateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var template = await _recurringJobService.CreateTemplateAsync(userId, request, cancellationToken);
            var dto = await _recurringJobService.GetTemplateAsync(template.Id, cancellationToken);
            return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Update a recurring job template
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<RecurringJobTemplateDto>> UpdateTemplate(
        Guid id,
        [FromBody] UpdateRecurringJobTemplateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var template = await _recurringJobService.UpdateTemplateAsync(userId, id, request, cancellationToken);

            if (template == null)
            {
                return NotFound();
            }

            var dto = await _recurringJobService.GetTemplateAsync(template.Id, cancellationToken);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Deactivate a recurring job template
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeactivateTemplate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _recurringJobService.DeactivateTemplateAsync(userId, id, cancellationToken);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Reactivate a recurring job template
    /// </summary>
    [HttpPost("{id}/activate")]
    public async Task<ActionResult> ActivateTemplate(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _recurringJobService.ActivateTemplateAsync(userId, id, cancellationToken);

            if (!result)
            {
                return NotFound();
            }

            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Manually generate the next job from a template
    /// </summary>
    [HttpPost("{id}/generate")]
    public async Task<ActionResult<GenerateJobsResult>> GenerateJob(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _recurringJobService.GenerateJobsForTemplateAsync(id, cancellationToken);

        if (result.Errors.Any())
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get job history for a recurring template
    /// </summary>
    [HttpGet("{id}/history")]
    public async Task<ActionResult<RecurringJobHistoryResponse>> GetJobHistory(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _recurringJobService.GetJobHistoryAsync(id, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Generate all due jobs (for background job or admin use)
    /// </summary>
    [HttpPost("generate-due")]
    public async Task<ActionResult<GenerateJobsResult>> GenerateDueJobs(
        CancellationToken cancellationToken)
    {
        var result = await _recurringJobService.GenerateDueJobsAsync(cancellationToken);
        return Ok(result);
    }
}
