using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.Messaging.Queries.GetConversations;

public record GetConversationsQuery : IRequest<Result<ConversationListResponse>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
