using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Enums;
using Quote.Shared.DTOs;

namespace Quote.Application.Disputes.Queries.GetDisputeById;

public class GetDisputeByIdQueryHandler : IRequestHandler<GetDisputeByIdQuery, Result<DisputeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetDisputeByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<DisputeDto>> Handle(GetDisputeByIdQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<DisputeDto>.Failure("User not authenticated");
        }

        var dispute = await _context.Disputes
            .Include(d => d.Job)
            .Include(d => d.JobQuote)
            .Include(d => d.RaisedByUser)
            .Include(d => d.ResolvedByUser)
            .Include(d => d.Evidence)
                .ThenInclude(e => e.UploadedByUser)
            .FirstOrDefaultAsync(d => d.Id == request.DisputeId, cancellationToken);

        if (dispute == null)
        {
            return Result<DisputeDto>.Failure("Dispute not found");
        }

        // Check authorization
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user == null)
        {
            return Result<DisputeDto>.Failure("User not found");
        }

        var isAdmin = user.UserType == UserType.Admin;
        var isCustomer = dispute.Job.CustomerId == _currentUser.UserId;
        var isTradie = dispute.JobQuote.TradieId == _currentUser.UserId;

        if (!isAdmin && !isCustomer && !isTradie)
        {
            return Result<DisputeDto>.Failure("You are not authorized to view this dispute");
        }

        // Get other party info
        Guid? otherPartyId = null;
        string? otherPartyName = null;
        if (dispute.RaisedByUserId == dispute.Job.CustomerId)
        {
            // Customer raised - other party is tradie
            var tradie = await _context.Users.FirstOrDefaultAsync(u => u.Id == dispute.JobQuote.TradieId, cancellationToken);
            otherPartyId = tradie?.Id;
            otherPartyName = tradie != null ? $"{tradie.FirstName} {tradie.LastName}" : null;
        }
        else
        {
            // Tradie raised - other party is customer
            var customer = await _context.Users.FirstOrDefaultAsync(u => u.Id == dispute.Job.CustomerId, cancellationToken);
            otherPartyId = customer?.Id;
            otherPartyName = customer != null ? $"{customer.FirstName} {customer.LastName}" : null;
        }

        var dto = new DisputeDto(
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
        );

        return Result<DisputeDto>.Success(dto);
    }
}
