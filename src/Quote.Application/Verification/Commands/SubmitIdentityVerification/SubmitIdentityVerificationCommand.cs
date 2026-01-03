using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Entities;
using Quote.Domain.Enums;

namespace Quote.Application.Verification.Commands.SubmitIdentityVerification;

public record SubmitIdentityVerificationCommand : IRequest<Result<SubmitVerificationResponse>>
{
    public Guid UserId { get; init; }
    public IdentityDocumentType DocumentType { get; init; }
    public Stream DocumentFrontStream { get; init; } = null!;
    public string DocumentFrontFileName { get; init; } = string.Empty;
    public string DocumentFrontContentType { get; init; } = string.Empty;
    public Stream? DocumentBackStream { get; init; }
    public string? DocumentBackFileName { get; init; }
    public string? DocumentBackContentType { get; init; }
    public string? DocumentNumber { get; init; }
    public string? IssuingState { get; init; }
    public DateTime? ExpiryDate { get; init; }
}

public record SubmitVerificationResponse
{
    public Guid VerificationId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

public class SubmitIdentityVerificationCommandHandler : IRequestHandler<SubmitIdentityVerificationCommand, Result<SubmitVerificationResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IBlobStorageService _blobStorage;

    public SubmitIdentityVerificationCommandHandler(IApplicationDbContext context, IBlobStorageService blobStorage)
    {
        _context = context;
        _blobStorage = blobStorage;
    }

    public async Task<Result<SubmitVerificationResponse>> Handle(SubmitIdentityVerificationCommand request, CancellationToken cancellationToken)
    {
        // Check if user exists and is a tradie
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return Result<SubmitVerificationResponse>.Failure(new[] { "User not found." });
        }

        if (user.UserType != UserType.Tradie)
        {
            return Result<SubmitVerificationResponse>.Failure(new[] { "Only tradies can submit verification documents." });
        }

        // Check if there's already a pending verification of this type
        var existingPending = await _context.IdentityVerifications
            .FirstOrDefaultAsync(v => v.UserId == request.UserId
                && v.DocumentType == request.DocumentType
                && v.VerificationStatus == VerificationStatus.Pending, cancellationToken);

        if (existingPending != null)
        {
            return Result<SubmitVerificationResponse>.Failure(new[] { "You already have a pending verification of this type. Please wait for it to be reviewed." });
        }

        // Upload front document
        var frontUrl = await _blobStorage.UploadAsync(
            request.DocumentFrontStream,
            request.DocumentFrontFileName,
            request.DocumentFrontContentType,
            cancellationToken);

        // Upload back document if provided
        string? backUrl = null;
        if (request.DocumentBackStream != null && !string.IsNullOrEmpty(request.DocumentBackFileName))
        {
            backUrl = await _blobStorage.UploadAsync(
                request.DocumentBackStream,
                request.DocumentBackFileName,
                request.DocumentBackContentType ?? "image/jpeg",
                cancellationToken);
        }

        // Create verification record
        var verification = new IdentityVerification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            DocumentType = request.DocumentType,
            DocumentFrontUrl = frontUrl,
            DocumentBackUrl = backUrl,
            DocumentNumber = request.DocumentNumber,
            IssuingState = request.IssuingState,
            ExpiryDate = request.ExpiryDate,
            VerificationStatus = VerificationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.IdentityVerifications.Add(verification);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<SubmitVerificationResponse>.Success(new SubmitVerificationResponse
        {
            VerificationId = verification.Id,
            Status = "Pending",
            Message = "Your documents have been submitted for review. You will be notified once the verification is complete."
        });
    }
}
