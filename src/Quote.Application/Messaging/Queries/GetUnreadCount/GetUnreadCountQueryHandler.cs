using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.Messaging.Queries.GetUnreadCount;

public class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, Result<UnreadCountDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetUnreadCountQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<UnreadCountDto>> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<UnreadCountDto>.Failure("User not authenticated");
        }

        var userId = _currentUser.UserId.Value;

        // Get all conversations where user is a participant
        var myParticipations = await _context.ConversationParticipants
            .Where(p => p.UserId == userId)
            .Select(p => new { p.ConversationId, p.LastReadAt })
            .ToListAsync(cancellationToken);

        var conversationIds = myParticipations.Select(p => p.ConversationId).ToList();

        // Count unread messages across all conversations
        var totalUnread = 0;
        foreach (var participation in myParticipations)
        {
            var unreadInConversation = await _context.Messages
                .Where(m => m.ConversationId == participation.ConversationId)
                .Where(m => m.SenderId != userId)
                .Where(m => participation.LastReadAt == null || m.SentAt > participation.LastReadAt)
                .CountAsync(cancellationToken);

            totalUnread += unreadInConversation;
        }

        return Result<UnreadCountDto>.Success(new UnreadCountDto(TotalUnread: totalUnread));
    }
}
