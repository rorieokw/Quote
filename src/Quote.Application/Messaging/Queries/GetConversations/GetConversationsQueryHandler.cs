using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.Messaging.Queries.GetConversations;

public class GetConversationsQueryHandler : IRequestHandler<GetConversationsQuery, Result<ConversationListResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetConversationsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<ConversationListResponse>> Handle(GetConversationsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<ConversationListResponse>.Failure("User not authenticated");
        }

        var userId = _currentUser.UserId.Value;

        // Get conversations where user is a participant
        var query = _context.Conversations
            .Include(c => c.Job)
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .ThenInclude(m => m.Sender)
            .Where(c => c.Participants.Any(p => p.UserId == userId));

        var totalCount = await query.CountAsync(cancellationToken);

        var conversations = await query
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var conversationDtos = conversations.Select(c =>
        {
            var otherParticipant = c.Participants.FirstOrDefault(p => p.UserId != userId);
            var lastMessage = c.Messages.FirstOrDefault();
            var myParticipant = c.Participants.FirstOrDefault(p => p.UserId == userId);

            // Calculate unread count - messages sent after my LastReadAt
            var unreadCount = 0;
            if (myParticipant?.LastReadAt != null)
            {
                unreadCount = c.Messages.Count(m =>
                    m.SenderId != userId &&
                    m.SentAt > myParticipant.LastReadAt);
            }
            else
            {
                // If never read, count all messages from others
                unreadCount = c.Messages.Count(m => m.SenderId != userId);
            }

            return new ConversationDto(
                Id: c.Id,
                JobId: c.JobId,
                JobTitle: c.Job.Title,
                OtherPartyName: otherParticipant?.User?.FullName ?? "Unknown",
                OtherPartyAvatar: null, // Can add avatar URL later
                LastMessage: lastMessage?.Content,
                LastMessageAt: c.LastMessageAt,
                UnreadCount: unreadCount,
                IsOnline: false // Can implement presence later
            );
        }).ToList();

        return Result<ConversationListResponse>.Success(new ConversationListResponse(
            Conversations: conversationDtos,
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize
        ));
    }
}
