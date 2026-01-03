using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Entities;
using Quote.Shared.DTOs;

namespace Quote.Application.Messaging.Commands.SendMessage;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Result<MessageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IMessageNotificationService? _notificationService;

    public SendMessageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IMessageNotificationService? notificationService = null)
    {
        _context = context;
        _currentUser = currentUser;
        _notificationService = notificationService;
    }

    public async Task<Result<MessageDto>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<MessageDto>.Failure("User not authenticated");
        }

        var userId = _currentUser.UserId.Value;

        // Verify user is a participant
        var conversation = await _context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken);

        if (conversation == null)
        {
            return Result<MessageDto>.Failure("Conversation not found");
        }

        if (!conversation.Participants.Any(p => p.UserId == userId))
        {
            return Result<MessageDto>.Failure("You are not a participant in this conversation");
        }

        // Get sender info
        var sender = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (sender == null)
        {
            return Result<MessageDto>.Failure("User not found");
        }

        // Create message
        var message = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = request.ConversationId,
            SenderId = userId,
            Content = request.Content,
            MediaUrl = request.MediaUrl,
            MediaType = request.MediaType,
            FileName = request.FileName,
            SentAt = DateTime.UtcNow,
            IsSystemMessage = false
        };

        _context.Messages.Add(message);

        // Update conversation last message time
        conversation.LastMessageAt = message.SentAt;

        await _context.SaveChangesAsync(cancellationToken);

        var messageDto = new MessageDto(
            Id: message.Id,
            SenderId: message.SenderId,
            SenderName: sender.FullName,
            Content: message.Content,
            MediaUrl: message.MediaUrl,
            MediaType: message.MediaType,
            FileName: message.FileName,
            SentAt: message.SentAt,
            ReadAt: null,
            IsSystemMessage: false,
            IsMine: true
        );

        // Broadcast message via SignalR (if service is available)
        if (_notificationService != null)
        {
            await _notificationService.NotifyMessageSentAsync(request.ConversationId, messageDto, cancellationToken);
        }

        return Result<MessageDto>.Success(messageDto);
    }
}
