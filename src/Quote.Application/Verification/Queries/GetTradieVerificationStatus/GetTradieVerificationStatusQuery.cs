using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Domain.Enums;

namespace Quote.Application.Verification.Queries.GetTradieVerificationStatus;

public record GetTradieVerificationStatusQuery : IRequest<TradieVerificationStatusDto?>
{
    public Guid UserId { get; init; }
}

public record TradieVerificationStatusDto
{
    public bool IdentityVerified { get; init; }
    public string? IdentityStatus { get; init; }
    public DateTime? IdentitySubmittedAt { get; init; }
    public string? IdentityNotes { get; init; }

    public bool InsuranceVerified { get; init; }
    public string? InsuranceStatus { get; init; }
    public DateTime? InsuranceSubmittedAt { get; init; }
    public DateTime? InsuranceExpiryDate { get; init; }
    public string? InsuranceNotes { get; init; }

    public bool PoliceCheckVerified { get; init; }
    public string? PoliceCheckStatus { get; init; }
    public DateTime? PoliceCheckSubmittedAt { get; init; }
    public string? PoliceCheckNotes { get; init; }

    public List<LicenceVerificationStatusDto> Licences { get; init; } = new();

    public string OverallVerificationLevel { get; init; } = "None";
    public List<string> EarnedBadges { get; init; } = new();
}

public record LicenceVerificationStatusDto
{
    public Guid Id { get; init; }
    public string TradeCategory { get; init; } = string.Empty;
    public string LicenceNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? VerifiedAt { get; init; }
    public string? Notes { get; init; }
}

public class GetTradieVerificationStatusQueryHandler : IRequestHandler<GetTradieVerificationStatusQuery, TradieVerificationStatusDto?>
{
    private readonly IApplicationDbContext _context;

    public GetTradieVerificationStatusQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TradieVerificationStatusDto?> Handle(GetTradieVerificationStatusQuery request, CancellationToken cancellationToken)
    {
        var tradieProfile = await _context.TradieProfiles
            .Include(tp => tp.Licences)
                .ThenInclude(l => l.TradeCategory)
            .FirstOrDefaultAsync(tp => tp.UserId == request.UserId, cancellationToken);

        if (tradieProfile == null)
        {
            return null;
        }

        // Get identity verification (latest)
        var identityVerification = await _context.IdentityVerifications
            .Where(v => v.UserId == request.UserId &&
                (v.DocumentType == IdentityDocumentType.DriversLicence ||
                 v.DocumentType == IdentityDocumentType.Passport ||
                 v.DocumentType == IdentityDocumentType.ProofOfAge ||
                 v.DocumentType == IdentityDocumentType.PhotoCard))
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        // Get insurance verification (latest)
        var insuranceVerification = await _context.IdentityVerifications
            .Where(v => v.UserId == request.UserId &&
                v.DocumentType == IdentityDocumentType.InsuranceCertificate)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        // Get police check verification (latest)
        var policeVerification = await _context.IdentityVerifications
            .Where(v => v.UserId == request.UserId &&
                v.DocumentType == IdentityDocumentType.PoliceCheckCertificate)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        // Build licence status list
        var licences = tradieProfile.Licences.Select(l => new LicenceVerificationStatusDto
        {
            Id = l.Id,
            TradeCategory = l.TradeCategory.Name,
            LicenceNumber = l.LicenceNumber,
            Status = l.VerificationStatus.ToString(),
            VerifiedAt = l.VerifiedAt,
            Notes = l.VerificationNotes
        }).ToList();

        // Calculate earned badges
        var badges = new List<string>();
        if (tradieProfile.IdentityVerified) badges.Add("ID Verified");
        if (tradieProfile.InsuranceVerified) badges.Add("Insured");
        if (tradieProfile.PoliceCheckVerified) badges.Add("Police Checked");

        var verifiedLicences = tradieProfile.Licences
            .Where(l => l.VerificationStatus == VerificationStatus.Verified)
            .ToList();
        foreach (var licence in verifiedLicences)
        {
            badges.Add($"Licensed {licence.TradeCategory.Name}");
        }

        // Calculate verification level
        var level = "None";
        if (tradieProfile.IdentityVerified)
        {
            level = "Basic";
            if (verifiedLicences.Any())
            {
                level = "Verified";
                if (tradieProfile.InsuranceVerified)
                {
                    level = "Premium";
                }
            }
        }

        return new TradieVerificationStatusDto
        {
            IdentityVerified = tradieProfile.IdentityVerified,
            IdentityStatus = identityVerification?.VerificationStatus.ToString(),
            IdentitySubmittedAt = identityVerification?.CreatedAt,
            IdentityNotes = identityVerification?.VerificationStatus == VerificationStatus.Rejected
                ? identityVerification.VerificationNotes : null,

            InsuranceVerified = tradieProfile.InsuranceVerified,
            InsuranceStatus = insuranceVerification?.VerificationStatus.ToString(),
            InsuranceSubmittedAt = insuranceVerification?.CreatedAt,
            InsuranceExpiryDate = tradieProfile.InsuranceExpiryDate,
            InsuranceNotes = insuranceVerification?.VerificationStatus == VerificationStatus.Rejected
                ? insuranceVerification.VerificationNotes : null,

            PoliceCheckVerified = tradieProfile.PoliceCheckVerified,
            PoliceCheckStatus = policeVerification?.VerificationStatus.ToString(),
            PoliceCheckSubmittedAt = policeVerification?.CreatedAt,
            PoliceCheckNotes = policeVerification?.VerificationStatus == VerificationStatus.Rejected
                ? policeVerification.VerificationNotes : null,

            Licences = licences,
            OverallVerificationLevel = level,
            EarnedBadges = badges
        };
    }
}
