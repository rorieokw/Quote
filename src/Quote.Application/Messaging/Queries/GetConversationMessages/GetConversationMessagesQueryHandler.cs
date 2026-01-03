using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.Messaging.Queries.GetConversationMessages;

public class GetConversationMessagesQueryHandler : IRequestHandler<GetConversationMessagesQuery, Result<ConversationDetailDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetConversationMessagesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<ConversationDetailDto>> Handle(GetConversationMessagesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<ConversationDetailDto>.Failure("User not authenticated");
        }

        var userId = _currentUser.UserId.Value;

        var conversation = await _context.Conversations
            .Include(c => c.Job)
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken);

        if (conversation == null)
        {
            return Result<ConversationDetailDto>.Failure("Conversation not found");
        }

        // Verify user is a participant
        if (!conversation.Participants.Any(p => p.UserId == userId))
        {
            return Result<ConversationDetailDto>.Failure("You are not a participant in this conversation");
        }

        // Get total message count
        var totalMessages = await _context.Messages
            .Where(m => m.ConversationId == request.ConversationId)
            .CountAsync(cancellationToken);

        // Get messages with pagination (most recent first, then reverse for display)
        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == request.ConversationId)
            .OrderByDescending(m => m.SentAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Reverse to show oldest first in the page
        messages.Reverse();

        var messageDtos = messages.Select(m => new MessageDto(
            Id: m.Id,
            SenderId: m.SenderId,
            SenderName: m.Sender.FullName,
            Content: m.Content,
            MediaUrl: m.MediaUrl,
            MediaType: m.MediaType,
            FileName: m.FileName,
            SentAt: m.SentAt,
            ReadAt: m.ReadAt,
            IsSystemMessage: m.IsSystemMessage,
            IsMine: m.SenderId == userId
        )).ToList();

        var otherParticipant = conversation.Participants.FirstOrDefault(p => p.UserId != userId);
        var otherPartyDto = otherParticipant != null
            ? new ParticipantDto(
                UserId: otherParticipant.UserId,
                Name: otherParticipant.User.FullName,
                AvatarUrl: null,
                UserType: otherParticipant.User.UserType.ToString(),
                LastReadAt: otherParticipant.LastReadAt
            )
            : new ParticipantDto(Guid.Empty, "Unknown", null, "Unknown", null);

        return Result<ConversationDetailDto>.Success(new ConversationDetailDto(
            Id: conversation.Id,
            JobId: conversation.JobId,
            JobTitle: conversation.Job.Title,
            JobStatus: conversation.Job.Status.ToString(),
            OtherParty: otherPartyDto,
            Messages: messageDtos,
            TotalMessages: totalMessages
        ));
    }
}
