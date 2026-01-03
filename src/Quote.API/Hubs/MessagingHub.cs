using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Quote.Application.Messaging.Commands.MarkMessagesAsRead;
using Quote.Application.Messaging.Commands.SendMessage;
using Quote.Shared.DTOs;

namespace Quote.API.Hubs;

[Authorize]
public class MessagingHub : Hub
{
    private readonly IMediator _mediator;
    private readonly ILogger<MessagingHub> _logger;

    // Track user connections (userId -> connectionIds)
    private static readonly Dictionary<Guid, HashSet<string>> UserConnections = new();
    private static readonly object ConnectionLock = new();

    public MessagingHub(IMediator mediator, ILogger<MessagingHub> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            lock (ConnectionLock)
            {
                if (!UserConnections.ContainsKey(userId.Value))
                {
                    UserConnections[userId.Value] = new HashSet<string>();
                }
                UserConnections[userId.Value].Add(Context.ConnectionId);
            }

            // Add to user's personal group for direct notifications
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId.Value}");

            _logger.LogInformation("User {UserId} connected with connection {ConnectionId}", userId.Value, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            lock (ConnectionLock)
            {
                if (UserConnections.ContainsKey(userId.Value))
                {
                    UserConnections[userId.Value].Remove(Context.ConnectionId);
                    if (UserConnections[userId.Value].Count == 0)
                    {
                        UserConnections.Remove(userId.Value);
                    }
                }
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId.Value}");

            _logger.LogInformation("User {UserId} disconnected", userId.Value);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a conversation group to receive real-time messages
    /// </summary>
    public async Task JoinConversation(Guid conversationId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            throw new HubException("Not authenticated");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        _logger.LogInformation("User {UserId} joined conversation {ConversationId}", userId.Value, conversationId);
    }

    /// <summary>
    /// Leave a conversation group
    /// </summary>
    public async Task LeaveConversation(Guid conversationId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            throw new HubException("Not authenticated");
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        _logger.LogInformation("User {UserId} left conversation {ConversationId}", userId.Value, conversationId);
    }

    /// <summary>
    /// Send a message through SignalR (alternative to HTTP endpoint)
    /// </summary>
    public async Task SendMessage(Guid conversationId, string content, string? mediaUrl = null, string? mediaType = null, string? fileName = null)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            throw new HubException("Not authenticated");
        }

        var command = new SendMessageCommand
        {
            ConversationId = conversationId,
            Content = content,
            MediaUrl = mediaUrl,
            MediaType = mediaType,
            FileName = fileName
        };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            throw new HubException(result.Errors.FirstOrDefault() ?? "Failed to send message");
        }

        // Broadcast to conversation group
        await Clients.Group($"conversation_{conversationId}")
            .SendAsync("ReceiveMessage", result.Data);

        _logger.LogInformation("Message sent in conversation {ConversationId} by user {UserId}", conversationId, userId.Value);
    }

    /// <summary>
    /// Mark messages as read and notify other participants
    /// </summary>
    public async Task MarkAsRead(Guid conversationId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            throw new HubException("Not authenticated");
        }

        var command = new MarkMessagesAsReadCommand
        {
            ConversationId = conversationId
        };

        var result = await _mediator.Send(command);

        if (result.Succeeded)
        {
            // Notify other participants that messages were read
            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("MessagesRead", conversationId, userId.Value);
        }
    }

    /// <summary>
    /// Notify typing indicator
    /// </summary>
    public async Task StartTyping(Guid conversationId)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("UserTyping", conversationId, userId.Value, true);
    }

    /// <summary>
    /// Stop typing indicator
    /// </summary>
    public async Task StopTyping(Guid conversationId)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        await Clients.OthersInGroup($"conversation_{conversationId}")
            .SendAsync("UserTyping", conversationId, userId.Value, false);
    }

    private Guid? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }

    /// <summary>
    /// Check if a user is currently online
    /// </summary>
    public static bool IsUserOnline(Guid userId)
    {
        lock (ConnectionLock)
        {
            return UserConnections.ContainsKey(userId) && UserConnections[userId].Count > 0;
        }
    }

    /// <summary>
    /// Get all connection IDs for a user
    /// </summary>
    public static IEnumerable<string> GetUserConnections(Guid userId)
    {
        lock (ConnectionLock)
        {
            if (UserConnections.TryGetValue(userId, out var connections))
            {
                return connections.ToList();
            }
            return Enumerable.Empty<string>();
        }
    }
}
