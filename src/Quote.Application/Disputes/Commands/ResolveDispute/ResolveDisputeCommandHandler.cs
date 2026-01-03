using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Enums;

namespace Quote.Application.Disputes.Commands.ResolveDispute;

public class ResolveDisputeCommandHandler : IRequestHandler<ResolveDisputeCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ResolveDisputeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ResolveDisputeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result.Failure("User not authenticated");
        }

        // Verify user is admin
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user == null || user.UserType != UserType.Admin)
        {
            return Result.Failure("Only admins can resolve disputes");
        }

        var dispute = await _context.Disputes
            .Include(d => d.JobQuote)
            .FirstOrDefaultAsync(d => d.Id == request.DisputeId, cancellationToken);

        if (dispute == null)
        {
            return Result.Failure("Dispute not found");
        }

        if (dispute.Status == DisputeStatus.Resolved || dispute.Status == DisputeStatus.Closed)
        {
            return Result.Failure("Dispute is already resolved or closed");
        }

        // Parse resolution type
        if (!Enum.TryParse<DisputeResolutionType>(request.ResolutionType, ignoreCase: true, out var resolutionType))
        {
            return Result.Failure("Invalid resolution type");
        }

        // Validate refund amount for partial refund
        if (resolutionType == DisputeResolutionType.PartialRefund && !request.RefundAmount.HasValue)
        {
            return Result.Failure("Refund amount is required for partial refunds");
        }

        if (resolutionType == DisputeResolutionType.FullRefund)
        {
            // Set refund amount to full quote amount
            dispute.RefundAmount = dispute.JobQuote.TotalCost;
        }
        else if (resolutionType == DisputeResolutionType.PartialRefund)
        {
            if (request.RefundAmount > dispute.JobQuote.TotalCost)
            {
                return Result.Failure("Refund amount cannot exceed quote amount");
            }
            dispute.RefundAmount = request.RefundAmount;
        }

        dispute.Status = DisputeStatus.Resolved;
        dispute.ResolutionType = resolutionType;
        dispute.Resolution = request.Resolution;
        dispute.AdminNotes = request.AdminNotes;
        dispute.ResolvedByUserId = _currentUser.UserId;
        dispute.ResolvedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
