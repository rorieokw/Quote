using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.Verification.Commands.ReviewIdentityVerification;
using Quote.Application.Verification.Queries.GetPendingVerifications;
using Quote.Shared.DTOs;
using System.Security.Claims;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get pending verifications for admin review queue
    /// </summary>
    [HttpGet("verifications/pending")]
    [ProducesResponseType(typeof(PendingVerificationsListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingVerifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? type = null)
    {
        var query = new GetPendingVerificationsQuery
        {
            PageNumber = page,
            PageSize = pageSize,
            VerificationType = type
        };

        var result = await _mediator.Send(query);

        return Ok(new PendingVerificationsListDto
        {
            Items = result.Items.Select(v => new PendingVerificationItemDto
            {
                Id = v.Id,
                VerificationType = v.VerificationType,
                UserId = v.UserId,
                TradieName = v.TradieName,
                TradieEmail = v.TradieEmail,
                BusinessName = v.BusinessName,
                DocumentType = v.DocumentType,
                DocumentFrontUrl = v.DocumentFrontUrl,
                DocumentBackUrl = v.DocumentBackUrl,
                DocumentNumber = v.DocumentNumber,
                IssuingState = v.IssuingState,
                ExpiryDate = v.ExpiryDate,
                SubmittedAt = v.SubmittedAt,
                DaysPending = v.DaysPending
            }).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            TotalPages = result.TotalPages
        });
    }

    /// <summary>
    /// Review and approve/reject an identity verification
    /// </summary>
    [HttpPost("verifications/{id:guid}/review")]
    [ProducesResponseType(typeof(ReviewVerificationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReviewVerification(Guid id, [FromBody] ReviewVerificationRequest request)
    {
        var reviewerId = GetUserId();
        if (reviewerId == null) return Unauthorized();

        var command = new ReviewIdentityVerificationCommand
        {
            VerificationId = id,
            ReviewerId = reviewerId.Value,
            Approved = request.Approved,
            Notes = request.Notes
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

        return Ok(new ReviewVerificationResponseDto
        {
            VerificationId = result.Data!.VerificationId,
            Status = result.Data.Status,
            TradieName = result.Data.TradieName
        });
    }

    /// <summary>
    /// Get verification statistics for admin dashboard
    /// </summary>
    [HttpGet("verifications/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetVerificationStats()
    {
        // Get counts for different verification types
        var identityPending = await _mediator.Send(new GetPendingVerificationsQuery
        {
            PageNumber = 1,
            PageSize = 1,
            VerificationType = "identity"
        });

        var insurancePending = await _mediator.Send(new GetPendingVerificationsQuery
        {
            PageNumber = 1,
            PageSize = 1,
            VerificationType = "insurance"
        });

        var policePending = await _mediator.Send(new GetPendingVerificationsQuery
        {
            PageNumber = 1,
            PageSize = 1,
            VerificationType = "police"
        });

        return Ok(new
        {
            pendingIdentity = identityPending.TotalCount,
            pendingInsurance = insurancePending.TotalCount,
            pendingPoliceCheck = policePending.TotalCount,
            totalPending = identityPending.TotalCount + insurancePending.TotalCount + policePending.TotalCount
        });
    }

    private Guid? GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return null;
        }
        return userId;
    }
}
