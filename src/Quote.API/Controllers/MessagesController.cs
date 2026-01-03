using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.Messaging.Commands.CreateConversation;
using Quote.Application.Messaging.Commands.MarkMessagesAsRead;
using Quote.Application.Messaging.Commands.SendMessage;
using Quote.Application.Messaging.Queries.GetConversationMessages;
using Quote.Application.Messaging.Queries.GetConversations;
using Quote.Application.Messaging.Queries.GetUnreadCount;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MessagesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all conversations for the current user
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(ConversationListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetConversations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetConversationsQuery
        {
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get a conversation with its messages
    /// </summary>
    [HttpGet("conversations/{id}")]
    [ProducesResponseType(typeof(ConversationDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetConversation(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = new GetConversationMessagesQuery
        {
            ConversationId = id,
            Page = page,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);

        if (!result.Succeeded)
        {
            return result.Errors.Contains("Conversation not found")
                ? NotFound(new { error = result.Errors.First() })
                : BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Create or get an existing conversation for a job
    /// </summary>
    [HttpPost("conversations")]
    [ProducesResponseType(typeof(CreateConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        var command = new CreateConversationCommand
        {
            JobId = request.JobId,
            OtherUserId = request.OtherUserId,
            InitialMessage = request.InitialMessage
        };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Send a message in a conversation
    /// </summary>
    [HttpPost("send")]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
    {
        var command = new SendMessageCommand
        {
            ConversationId = request.ConversationId,
            Content = request.Content,
            MediaUrl = request.MediaUrl,
            MediaType = request.MediaType,
            FileName = request.FileName
        };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Mark all messages in a conversation as read
    /// </summary>
    [HttpPost("conversations/{id}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var command = new MarkMessagesAsReadCommand
        {
            ConversationId = id
        };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// Get total unread message count (for nav badge)
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadCountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var result = await _mediator.Send(new GetUnreadCountQuery());

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }
}
