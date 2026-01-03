using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.Messaging.Queries.GetUnreadCount;

public record GetUnreadCountQuery : IRequest<Result<UnreadCountDto>>;
