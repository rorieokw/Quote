using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Enums;

namespace Quote.Application.Verification.Queries.GetPendingVerifications;

public record GetPendingVerificationsQuery : IRequest<PaginatedList<PendingVerificationDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? VerificationType { get; init; } // "identity", "licence", "insurance", "police", or null for all
}

public record PendingVerificationDto
{
    public Guid Id { get; init; }
    public string VerificationType { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public string TradieName { get; init; } = string.Empty;
    public string TradieEmail { get; init; } = string.Empty;
    public string? BusinessName { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public string? DocumentFrontUrl { get; init; }
    public string? DocumentBackUrl { get; init; }
    public string? DocumentNumber { get; init; }
    public string? IssuingState { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public DateTime SubmittedAt { get; init; }
    public int DaysPending { get; init; }
}

public class GetPendingVerificationsQueryHandler : IRequestHandler<GetPendingVerificationsQuery, PaginatedList<PendingVerificationDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPendingVerificationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<PendingVerificationDto>> Handle(GetPendingVerificationsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.IdentityVerifications
            .Include(v => v.User)
                .ThenInclude(u => u.TradieProfile)
            .Where(v => v.VerificationStatus == VerificationStatus.Pending)
            .AsQueryable();

        // Filter by verification type if specified
        if (!string.IsNullOrEmpty(request.VerificationType))
        {
            query = request.VerificationType.ToLower() switch
            {
                "identity" => query.Where(v =>
                    v.DocumentType == IdentityDocumentType.DriversLicence ||
                    v.DocumentType == IdentityDocumentType.Passport ||
                    v.DocumentType == IdentityDocumentType.ProofOfAge ||
                    v.DocumentType == IdentityDocumentType.PhotoCard),
                "insurance" => query.Where(v => v.DocumentType == IdentityDocumentType.InsuranceCertificate),
                "police" => query.Where(v => v.DocumentType == IdentityDocumentType.PoliceCheckCertificate),
                _ => query
            };
        }

        // Order by oldest first (FIFO queue)
        query = query.OrderBy(v => v.CreatedAt);

        var projectedQuery = query.Select(v => new PendingVerificationDto
        {
            Id = v.Id,
            VerificationType = GetVerificationType(v.DocumentType),
            UserId = v.UserId,
            TradieName = $"{v.User.FirstName} {v.User.LastName}",
            TradieEmail = v.User.Email,
            BusinessName = v.User.TradieProfile != null ? v.User.TradieProfile.BusinessName : null,
            DocumentType = v.DocumentType.ToString(),
            DocumentFrontUrl = v.DocumentFrontUrl,
            DocumentBackUrl = v.DocumentBackUrl,
            DocumentNumber = v.DocumentNumber,
            IssuingState = v.IssuingState,
            ExpiryDate = v.ExpiryDate,
            SubmittedAt = v.CreatedAt,
            DaysPending = (int)(DateTime.UtcNow - v.CreatedAt).TotalDays
        });

        return await PaginatedList<PendingVerificationDto>.CreateAsync(
            projectedQuery, request.PageNumber, request.PageSize, cancellationToken);
    }

    private static string GetVerificationType(IdentityDocumentType docType) => docType switch
    {
        IdentityDocumentType.DriversLicence or
        IdentityDocumentType.Passport or
        IdentityDocumentType.ProofOfAge or
        IdentityDocumentType.PhotoCard => "Identity",
        IdentityDocumentType.InsuranceCertificate => "Insurance",
        IdentityDocumentType.PoliceCheckCertificate => "Police Check",
        _ => "Unknown"
    };
}
