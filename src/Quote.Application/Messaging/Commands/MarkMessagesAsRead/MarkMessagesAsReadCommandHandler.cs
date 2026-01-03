using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;

namespace Quote.Application.Messaging.Commands.MarkMessagesAsRead;

public class MarkMessagesAsReadCommandHandler : IRequestHandler<MarkMessagesAsReadCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public MarkMessagesAsReadCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(MarkMessagesAsReadCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<bool>.Failure("User not authenticated");
        }

        var userId = _currentUser.UserId.Value;

        // Find user's participation in this conversation
        var participation = await _context.ConversationParticipants
            .FirstOrDefaultAsync(p =>
                p.ConversationId == request.ConversationId &&
                p.UserId == userId,
                cancellationToken);

        if (participation == null)
        {
            return Result<bool>.Failure("You are not a participant in this conversation");
        }

        // Update LastReadAt to mark all messages as read
        participation.LastReadAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
