using MediatR;
using Quote.Application.Common.Models;

namespace Quote.Application.Messaging.Commands.CreateConversation;

public record CreateConversationCommand : IRequest<Result<CreateConversationResponse>>
{
    public Guid JobId { get; init; }
    public Guid OtherUserId { get; init; }
    public string? InitialMessage { get; init; }
}

public record CreateConversationResponse(
    Guid ConversationId,
    bool WasCreated
);
