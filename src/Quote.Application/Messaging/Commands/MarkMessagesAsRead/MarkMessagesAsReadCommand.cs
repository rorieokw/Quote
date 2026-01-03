using MediatR;
using Quote.Application.Common.Models;

namespace Quote.Application.Messaging.Commands.MarkMessagesAsRead;

public record MarkMessagesAsReadCommand : IRequest<Result<bool>>
{
    public Guid ConversationId { get; init; }
}
