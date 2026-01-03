using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.Scheduling.Services;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScheduleController : ControllerBase
{
    private readonly ISchedulingService _schedulingService;

    public ScheduleController(ISchedulingService schedulingService)
    {
        _schedulingService = schedulingService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    [HttpGet("day/{date}")]
    public async Task<ActionResult<List<ScheduleEventDto>>> GetDaySchedule(
        DateTime date,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var events = await _schedulingService.GetScheduleForDayAsync(userId, date, cancellationToken);
        return Ok(events);
    }

    [HttpGet("week")]
    public async Task<ActionResult<ScheduleWeekResponse>> GetWeekSchedule(
        [FromQuery] DateTime? weekStart,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var start = weekStart ?? DateTime.Today;
        var response = await _schedulingService.GetScheduleForWeekAsync(userId, start, cancellationToken);
        return Ok(response);
    }

    [HttpGet("month/{year}/{month}")]
    public async Task<ActionResult<ScheduleMonthResponse>> GetMonthSchedule(
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var response = await _schedulingService.GetScheduleForMonthAsync(userId, year, month, cancellationToken);
        return Ok(response);
    }

    [HttpPost("events")]
    public async Task<ActionResult<ScheduleEventDto>> CreateEvent(
        [FromBody] CreateScheduleEventRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var scheduleEvent = await _schedulingService.CreateEventAsync(userId, request, cancellationToken);

        return Ok(new ScheduleEventDto(
            Id: scheduleEvent.Id,
            Title: scheduleEvent.Title,
            Description: scheduleEvent.Description,
            EventType: scheduleEvent.EventType.ToString(),
            StartTime: scheduleEvent.StartTime,
            EndTime: scheduleEvent.EndTime,
            IsAllDay: scheduleEvent.IsAllDay,
            Address: scheduleEvent.Address,
            SuburbName: scheduleEvent.SuburbName,
            Latitude: scheduleEvent.Latitude,
            Longitude: scheduleEvent.Longitude,
            TravelTimeMinutes: scheduleEvent.TravelTimeMinutes,
            TravelDistanceKm: scheduleEvent.TravelDistanceKm,
            Color: scheduleEvent.Color,
            Notes: scheduleEvent.Notes,
            JobId: scheduleEvent.JobId,
            JobTitle: null,
            QuoteId: scheduleEvent.QuoteId,
            IsRecurring: scheduleEvent.IsRecurring
        ));
    }

    [HttpPut("events/{id}")]
    public async Task<ActionResult<ScheduleEventDto>> UpdateEvent(
        Guid id,
        [FromBody] UpdateScheduleEventRequest request,
        CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            return BadRequest("Event ID mismatch");
        }

        var userId = GetCurrentUserId();
        var scheduleEvent = await _schedulingService.UpdateEventAsync(userId, request, cancellationToken);

        if (scheduleEvent == null)
        {
            return NotFound();
        }

        return Ok(new ScheduleEventDto(
            Id: scheduleEvent.Id,
            Title: scheduleEvent.Title,
            Description: scheduleEvent.Description,
            EventType: scheduleEvent.EventType.ToString(),
            StartTime: scheduleEvent.StartTime,
            EndTime: scheduleEvent.EndTime,
            IsAllDay: scheduleEvent.IsAllDay,
            Address: scheduleEvent.Address,
            SuburbName: scheduleEvent.SuburbName,
            Latitude: scheduleEvent.Latitude,
            Longitude: scheduleEvent.Longitude,
            TravelTimeMinutes: scheduleEvent.TravelTimeMinutes,
            TravelDistanceKm: scheduleEvent.TravelDistanceKm,
            Color: scheduleEvent.Color,
            Notes: scheduleEvent.Notes,
            JobId: scheduleEvent.JobId,
            JobTitle: null,
            QuoteId: scheduleEvent.QuoteId,
            IsRecurring: scheduleEvent.IsRecurring
        ));
    }

    [HttpDelete("events/{id}")]
    public async Task<ActionResult> DeleteEvent(
        Guid id,
        [FromQuery] bool deleteSeries = false,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var deleted = await _schedulingService.DeleteEventAsync(userId, id, deleteSeries, cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("optimize")]
    public async Task<ActionResult<OptimizedScheduleResponse>> OptimizeDay(
        [FromBody] OptimizeDayRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var response = await _schedulingService.OptimizeDayScheduleAsync(userId, request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("conflicts")]
    public async Task<ActionResult<List<ScheduleConflictDto>>> GetConflicts(
        [FromQuery] DateTime start,
        [FromQuery] DateTime end,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var conflicts = await _schedulingService.GetConflictsAsync(userId, start, end, cancellationToken);
        return Ok(conflicts);
    }

    [HttpPost("availability")]
    public async Task<ActionResult<ScheduleAvailabilityResponse>> GetAvailability(
        [FromBody] ScheduleAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var response = await _schedulingService.GetAvailableSlotsAsync(userId, request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("quick-add-job")]
    public async Task<ActionResult<ScheduleEventDto>> QuickAddJob(
        [FromBody] QuickAddJobRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var scheduleEvent = await _schedulingService.QuickAddJobAsync(userId, request, cancellationToken);

        if (scheduleEvent == null)
        {
            return NotFound("Job not found");
        }

        return Ok(new ScheduleEventDto(
            Id: scheduleEvent.Id,
            Title: scheduleEvent.Title,
            Description: scheduleEvent.Description,
            EventType: scheduleEvent.EventType.ToString(),
            StartTime: scheduleEvent.StartTime,
            EndTime: scheduleEvent.EndTime,
            IsAllDay: scheduleEvent.IsAllDay,
            Address: scheduleEvent.Address,
            SuburbName: scheduleEvent.SuburbName,
            Latitude: scheduleEvent.Latitude,
            Longitude: scheduleEvent.Longitude,
            TravelTimeMinutes: scheduleEvent.TravelTimeMinutes,
            TravelDistanceKm: scheduleEvent.TravelDistanceKm,
            Color: scheduleEvent.Color,
            Notes: scheduleEvent.Notes,
            JobId: scheduleEvent.JobId,
            JobTitle: null,
            QuoteId: scheduleEvent.QuoteId,
            IsRecurring: scheduleEvent.IsRecurring
        ));
    }

    [HttpPost("recalculate-travel/{date}")]
    public async Task<ActionResult> RecalculateTravelTimes(
        DateTime date,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _schedulingService.CalculateTravelTimesAsync(userId, date, cancellationToken);
        return Ok();
    }
}
