using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Entities;
using Quote.Domain.Enums;

namespace Quote.Application.Disputes.Commands.CreateDispute;

public class CreateDisputeCommandHandler : IRequestHandler<CreateDisputeCommand, Result<CreateDisputeResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateDisputeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<CreateDisputeResponse>> Handle(CreateDisputeCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<CreateDisputeResponse>.Failure("User not authenticated");
        }

        // Get the quote and job
        var quote = await _context.Quotes
            .Include(q => q.Job)
            .FirstOrDefaultAsync(q => q.Id == request.JobQuoteId, cancellationToken);

        if (quote == null)
        {
            return Result<CreateDisputeResponse>.Failure("Quote not found");
        }

        // Verify user is involved (customer or tradie)
        var isCustomer = quote.Job.CustomerId == _currentUser.UserId;
        var isTradie = quote.TradieId == _currentUser.UserId;

        if (!isCustomer && !isTradie)
        {
            return Result<CreateDisputeResponse>.Failure("You are not authorized to file a dispute for this job");
        }

        // Verify quote is accepted (disputes only for accepted quotes)
        if (quote.Status != QuoteStatus.Accepted)
        {
            return Result<CreateDisputeResponse>.Failure("Disputes can only be filed for accepted quotes");
        }

        // Check for existing open dispute on same quote
        var existingDispute = await _context.Disputes
            .AnyAsync(d => d.JobQuoteId == request.JobQuoteId &&
                          (d.Status == DisputeStatus.Open || d.Status == DisputeStatus.UnderReview),
                     cancellationToken);

        if (existingDispute)
        {
            return Result<CreateDisputeResponse>.Failure("An open dispute already exists for this quote");
        }

        // Parse reason
        if (!Enum.TryParse<DisputeReason>(request.Reason, ignoreCase: true, out var reason))
        {
            return Result<CreateDisputeResponse>.Failure("Invalid dispute reason");
        }

        var dispute = new Dispute
        {
            Id = Guid.NewGuid(),
            JobId = quote.JobId,
            JobQuoteId = quote.Id,
            RaisedByUserId = _currentUser.UserId.Value,
            Reason = reason,
            Status = DisputeStatus.Open,
            Description = request.Description
        };

        _context.Disputes.Add(dispute);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<CreateDisputeResponse>.Success(new CreateDisputeResponse
        {
            DisputeId = dispute.Id
        });
    }
}
