using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Enums;
using Quote.Shared.DTOs;

namespace Quote.Application.Disputes.Queries.GetAllDisputes;

public class GetAllDisputesQueryHandler : IRequestHandler<GetAllDisputesQuery, Result<DisputeListResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetAllDisputesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<DisputeListResponse>> Handle(GetAllDisputesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<DisputeListResponse>.Failure("User not authenticated");
        }

        // Verify admin
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user == null || user.UserType != UserType.Admin)
        {
            return Result<DisputeListResponse>.Failure("Only admins can view all disputes");
        }

        var query = _context.Disputes
            .Include(d => d.Job)
            .Include(d => d.JobQuote)
            .Include(d => d.RaisedByUser)
            .Include(d => d.ResolvedByUser)
            .Include(d => d.Evidence)
                .ThenInclude(e => e.UploadedByUser)
            .AsQueryable();

        // Filter by status
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<DisputeStatus>(request.Status, true, out var status))
        {
            query = query.Where(d => d.Status == status);
        }

        // Filter by reason
        if (!string.IsNullOrEmpty(request.Reason) && Enum.TryParse<DisputeReason>(request.Reason, true, out var reason))
        {
            query = query.Where(d => d.Reason == reason);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var disputes = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = new List<DisputeDto>();
        foreach (var dispute in disputes)
        {
            // Get other party info
            Guid? otherPartyId = null;
            string? otherPartyName = null;
            if (dispute.RaisedByUserId == dispute.Job.CustomerId)
            {
                var tradie = await _context.Users.FirstOrDefaultAsync(u => u.Id == dispute.JobQuote.TradieId, cancellationToken);
                otherPartyId = tradie?.Id;
                otherPartyName = tradie != null ? $"{tradie.FirstName} {tradie.LastName}" : null;
            }
            else
            {
                var customer = await _context.Users.FirstOrDefaultAsync(u => u.Id == dispute.Job.CustomerId, cancellationToken);
                otherPartyId = customer?.Id;
                otherPartyName = customer != null ? $"{customer.FirstName} {customer.LastName}" : null;
            }

            dtos.Add(new DisputeDto(
                dispute.Id,
                dispute.JobId,
                dispute.Job.Title,
                dispute.JobQuoteId,
                dispute.JobQuote.TotalCost,
                dispute.RaisedByUserId,
                $"{dispute.RaisedByUser.FirstName} {dispute.RaisedByUser.LastName}",
                dispute.RaisedByUserId == dispute.Job.CustomerId ? "Customer" : "Tradie",
                otherPartyId,
                otherPartyName,
                dispute.Reason.ToString(),
                dispute.Status.ToString(),
                dispute.Description,
                dispute.AdminNotes,
                dispute.Resolution,
                dispute.ResolutionType?.ToString(),
                dispute.RefundAmount,
                dispute.ResolvedByUser != null ? $"{dispute.ResolvedByUser.FirstName} {dispute.ResolvedByUser.LastName}" : null,
                dispute.CreatedAt,
                dispute.ResolvedAt,
                dispute.Evidence.Select(e => new DisputeEvidenceDto(
                    e.Id,
                    e.FileName,
                    e.FileUrl,
                    e.Description,
                    $"{e.UploadedByUser.FirstName} {e.UploadedByUser.LastName}",
                    e.CreatedAt
                )).ToList()
            ));
        }

        return Result<DisputeListResponse>.Success(new DisputeListResponse(dtos, totalCount));
    }
}
