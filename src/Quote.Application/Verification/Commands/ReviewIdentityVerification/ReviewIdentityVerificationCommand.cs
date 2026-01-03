using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Enums;

namespace Quote.Application.Verification.Commands.ReviewIdentityVerification;

public record ReviewIdentityVerificationCommand : IRequest<Result<ReviewVerificationResponse>>
{
    public Guid VerificationId { get; init; }
    public Guid ReviewerId { get; init; }
    public bool Approved { get; init; }
    public string? Notes { get; init; }
}

public record ReviewVerificationResponse
{
    public Guid VerificationId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string TradieName { get; init; } = string.Empty;
}

public class ReviewIdentityVerificationCommandHandler : IRequestHandler<ReviewIdentityVerificationCommand, Result<ReviewVerificationResponse>>
{
    private readonly IApplicationDbContext _context;

    public ReviewIdentityVerificationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ReviewVerificationResponse>> Handle(ReviewIdentityVerificationCommand request, CancellationToken cancellationToken)
    {
        var verification = await _context.IdentityVerifications
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == request.VerificationId, cancellationToken);

        if (verification == null)
        {
            return Result<ReviewVerificationResponse>.Failure(new[] { "Verification not found." });
        }

        if (verification.VerificationStatus != VerificationStatus.Pending)
        {
            return Result<ReviewVerificationResponse>.Failure(new[] { "This verification has already been reviewed." });
        }

        // Update verification status
        verification.VerificationStatus = request.Approved ? VerificationStatus.Verified : VerificationStatus.Rejected;
        verification.VerifiedAt = DateTime.UtcNow;
        verification.VerificationNotes = request.Notes;
        verification.ReviewedByUserId = request.ReviewerId;
        verification.UpdatedAt = DateTime.UtcNow;

        // If approved, update the tradie profile based on document type
        if (request.Approved)
        {
            var tradieProfile = await _context.TradieProfiles
                .FirstOrDefaultAsync(tp => tp.UserId == verification.UserId, cancellationToken);

            if (tradieProfile != null)
            {
                switch (verification.DocumentType)
                {
                    case IdentityDocumentType.DriversLicence:
                    case IdentityDocumentType.Passport:
                    case IdentityDocumentType.ProofOfAge:
                    case IdentityDocumentType.PhotoCard:
                        tradieProfile.IdentityVerified = true;
                        break;
                    case IdentityDocumentType.InsuranceCertificate:
                        tradieProfile.InsuranceVerified = true;
                        tradieProfile.InsuranceExpiryDate = verification.ExpiryDate;
                        break;
                    case IdentityDocumentType.PoliceCheckCertificate:
                        tradieProfile.PoliceCheckVerified = true;
                        break;
                }
                tradieProfile.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<ReviewVerificationResponse>.Success(new ReviewVerificationResponse
        {
            VerificationId = verification.Id,
            Status = verification.VerificationStatus.ToString(),
            TradieName = $"{verification.User.FirstName} {verification.User.LastName}"
        });
    }
}
