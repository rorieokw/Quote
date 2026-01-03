using Quote.Shared.DTOs;

namespace Quote.Application.Common.Interfaces;

public interface IMessageNotificationService
{
    Task NotifyMessageSentAsync(Guid conversationId, MessageDto message, CancellationToken cancellationToken = default);
    Task NotifyMessagesReadAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);
}
