using Microsoft.AspNetCore.SignalR;
using Quote.API.Hubs;
using Quote.Application.Common.Interfaces;
using Quote.Shared.DTOs;

namespace Quote.API.Services;

public class MessageNotificationService : IMessageNotificationService
{
    private readonly IHubContext<MessagingHub> _hubContext;
    private readonly ILogger<MessageNotificationService> _logger;

    public MessageNotificationService(
        IHubContext<MessagingHub> hubContext,
        ILogger<MessageNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyMessageSentAsync(Guid conversationId, MessageDto message, CancellationToken cancellationToken = default)
    {
        try
        {
            // Notify all users in the conversation group
            await _hubContext.Clients
                .Group($"conversation_{conversationId}")
                .SendAsync("ReceiveMessage", message, cancellationToken);

            _logger.LogInformation("Notified message {MessageId} to conversation {ConversationId}",
                message.Id, conversationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify message {MessageId} to conversation {ConversationId}",
                message.Id, conversationId);
        }
    }

    public async Task NotifyMessagesReadAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients
                .Group($"conversation_{conversationId}")
                .SendAsync("MessagesRead", conversationId, userId, cancellationToken);

            _logger.LogInformation("Notified read receipt for conversation {ConversationId} by user {UserId}",
                conversationId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify read receipt for conversation {ConversationId}",
                conversationId);
        }
    }
}
