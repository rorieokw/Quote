using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.Verification.Commands.SubmitIdentityVerification;
using Quote.Application.Verification.Queries.GetTradieVerificationStatus;
using Quote.Domain.Enums;
using Quote.Shared.DTOs;
using System.Security.Claims;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VerificationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public VerificationController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    /// <summary>
    /// Get current user's verification status
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(TradieVerificationStatusResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var result = await _mediator.Send(new GetTradieVerificationStatusQuery { UserId = userId.Value });

        if (result == null)
        {
            return NotFound(new { error = "Tradie profile not found" });
        }

        return Ok(new TradieVerificationStatusResponseDto
        {
            IdentityVerified = result.IdentityVerified,
            IdentityStatus = result.IdentityStatus,
            IdentitySubmittedAt = result.IdentitySubmittedAt,
            IdentityNotes = result.IdentityNotes,
            InsuranceVerified = result.InsuranceVerified,
            InsuranceStatus = result.InsuranceStatus,
            InsuranceSubmittedAt = result.InsuranceSubmittedAt,
            InsuranceExpiryDate = result.InsuranceExpiryDate,
            InsuranceNotes = result.InsuranceNotes,
            PoliceCheckVerified = result.PoliceCheckVerified,
            PoliceCheckStatus = result.PoliceCheckStatus,
            PoliceCheckSubmittedAt = result.PoliceCheckSubmittedAt,
            PoliceCheckNotes = result.PoliceCheckNotes,
            Licences = result.Licences.Select(l => new LicenceStatusDto
            {
                Id = l.Id,
                TradeCategory = l.TradeCategory,
                LicenceNumber = l.LicenceNumber,
                Status = l.Status,
                VerifiedAt = l.VerifiedAt,
                Notes = l.Notes
            }).ToList(),
            OverallVerificationLevel = result.OverallVerificationLevel,
            EarnedBadges = result.EarnedBadges
        });
    }

    /// <summary>
    /// Submit identity document for verification (Driver's Licence, Passport, etc.)
    /// </summary>
    [HttpPost("identity")]
    [ProducesResponseType(typeof(SubmitVerificationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SubmitIdentityVerification(
        [FromForm] SubmitIdentityVerificationRequest request,
        [FromForm] IFormFile documentFront,
        [FromForm] IFormFile? documentBack = null)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var validationError = ValidateFile(documentFront);
        if (validationError != null) return BadRequest(new { error = validationError });

        if (documentBack != null)
        {
            validationError = ValidateFile(documentBack);
            if (validationError != null) return BadRequest(new { error = validationError });
        }

        // Validate document type is an identity document
        var docType = (IdentityDocumentType)request.DocumentType;
        if (docType != IdentityDocumentType.DriversLicence &&
            docType != IdentityDocumentType.Passport &&
            docType != IdentityDocumentType.ProofOfAge &&
            docType != IdentityDocumentType.PhotoCard)
        {
            return BadRequest(new { error = "Invalid document type for identity verification" });
        }

        using var frontStream = documentFront.OpenReadStream();
        using var backStream = documentBack?.OpenReadStream();

        var command = new SubmitIdentityVerificationCommand
        {
            UserId = userId.Value,
            DocumentType = docType,
            DocumentFrontStream = frontStream,
            DocumentFrontFileName = documentFront.FileName,
            DocumentFrontContentType = documentFront.ContentType,
            DocumentBackStream = backStream,
            DocumentBackFileName = documentBack?.FileName,
            DocumentBackContentType = documentBack?.ContentType,
            DocumentNumber = request.DocumentNumber,
            IssuingState = request.IssuingState,
            ExpiryDate = request.ExpiryDate
        };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new SubmitVerificationResponseDto
        {
            VerificationId = result.Data!.VerificationId,
            Status = result.Data.Status,
            Message = result.Data.Message
        });
    }

    /// <summary>
    /// Submit insurance certificate for verification
    /// </summary>
    [HttpPost("insurance")]
    [ProducesResponseType(typeof(SubmitVerificationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SubmitInsuranceVerification(
        [FromForm] IFormFile document,
        [FromForm] DateTime expiryDate,
        [FromForm] string? policyNumber = null)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var validationError = ValidateFile(document);
        if (validationError != null) return BadRequest(new { error = validationError });

        using var stream = document.OpenReadStream();

        var command = new SubmitIdentityVerificationCommand
        {
            UserId = userId.Value,
            DocumentType = IdentityDocumentType.InsuranceCertificate,
            DocumentFrontStream = stream,
            DocumentFrontFileName = document.FileName,
            DocumentFrontContentType = document.ContentType,
            DocumentNumber = policyNumber,
            ExpiryDate = expiryDate
        };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new SubmitVerificationResponseDto
        {
            VerificationId = result.Data!.VerificationId,
            Status = result.Data.Status,
            Message = result.Data.Message
        });
    }

    /// <summary>
    /// Submit police check certificate for verification
    /// </summary>
    [HttpPost("police-check")]
    [ProducesResponseType(typeof(SubmitVerificationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SubmitPoliceCheckVerification(
        [FromForm] IFormFile document,
        [FromForm] string? certificateNumber = null)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var validationError = ValidateFile(document);
        if (validationError != null) return BadRequest(new { error = validationError });

        using var stream = document.OpenReadStream();

        var command = new SubmitIdentityVerificationCommand
        {
            UserId = userId.Value,
            DocumentType = IdentityDocumentType.PoliceCheckCertificate,
            DocumentFrontStream = stream,
            DocumentFrontFileName = document.FileName,
            DocumentFrontContentType = document.ContentType,
            DocumentNumber = certificateNumber
        };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new SubmitVerificationResponseDto
        {
            VerificationId = result.Data!.VerificationId,
            Status = result.Data.Status,
            Message = result.Data.Message
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

    private string? ValidateFile(IFormFile file)
    {
        var maxSizeMB = _configuration.GetValue<int>("Storage:MaxFileSizeMB", 10);
        var maxSizeBytes = maxSizeMB * 1024 * 1024;

        if (file.Length > maxSizeBytes)
        {
            return $"File size exceeds maximum allowed ({maxSizeMB}MB)";
        }

        var allowedExtensions = _configuration.GetSection("Storage:AllowedExtensions").Get<string[]>()
            ?? new[] { ".jpg", ".jpeg", ".png", ".pdf" };

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return $"File type not allowed. Allowed types: {string.Join(", ", allowedExtensions)}";
        }

        return null;
    }
}
