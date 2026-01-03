using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Enums;

namespace Quote.Application.Disputes.Commands.CloseDispute;

public class CloseDisputeCommandHandler : IRequestHandler<CloseDisputeCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CloseDisputeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CloseDisputeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result.Failure("User not authenticated");
        }

        var dispute = await _context.Disputes
            .Include(d => d.Job)
            .Include(d => d.JobQuote)
            .FirstOrDefaultAsync(d => d.Id == request.DisputeId, cancellationToken);

        if (dispute == null)
        {
            return Result.Failure("Dispute not found");
        }

        // Check authorization - only the person who raised it or an admin can close
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user == null)
        {
            return Result.Failure("User not found");
        }

        var isAdmin = user.UserType == UserType.Admin;
        var isRaiser = dispute.RaisedByUserId == _currentUser.UserId;

        if (!isAdmin && !isRaiser)
        {
            return Result.Failure("You are not authorized to close this dispute");
        }

        if (dispute.Status == DisputeStatus.Resolved || dispute.Status == DisputeStatus.Closed)
        {
            return Result.Failure("Dispute is already resolved or closed");
        }

        dispute.Status = DisputeStatus.Closed;
        dispute.Resolution = request.Reason ?? "Dispute withdrawn";
        dispute.ResolvedAt = DateTime.UtcNow;

        if (isAdmin)
        {
            dispute.ResolvedByUserId = _currentUser.UserId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
