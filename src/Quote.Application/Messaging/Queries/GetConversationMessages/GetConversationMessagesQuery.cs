using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.Messaging.Queries.GetConversationMessages;

public record GetConversationMessagesQuery : IRequest<Result<ConversationDetailDto>>
{
    public Guid ConversationId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
