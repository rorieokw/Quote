using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.Messaging.Commands.SendMessage;

public record SendMessageCommand : IRequest<Result<MessageDto>>
{
    public Guid ConversationId { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? MediaUrl { get; init; }
    public string? MediaType { get; init; }
    public string? FileName { get; init; }
}
