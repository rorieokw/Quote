using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Entities;

namespace Quote.Application.Messaging.Commands.CreateConversation;

public class CreateConversationCommandHandler : IRequestHandler<CreateConversationCommand, Result<CreateConversationResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateConversationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<CreateConversationResponse>> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<CreateConversationResponse>.Failure("User not authenticated");
        }

        var userId = _currentUser.UserId.Value;

        // Verify job exists
        var job = await _context.Jobs.FindAsync(new object[] { request.JobId }, cancellationToken);
        if (job == null)
        {
            return Result<CreateConversationResponse>.Failure("Job not found");
        }

        // Verify other user exists
        var otherUser = await _context.Users.FindAsync(new object[] { request.OtherUserId }, cancellationToken);
        if (otherUser == null)
        {
            return Result<CreateConversationResponse>.Failure("User not found");
        }

        // Check if conversation already exists for this job between these two users
        var existingConversation = await _context.Conversations
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c =>
                c.JobId == request.JobId &&
                c.Participants.Any(p => p.UserId == userId) &&
                c.Participants.Any(p => p.UserId == request.OtherUserId),
                cancellationToken);

        if (existingConversation != null)
        {
            return Result<CreateConversationResponse>.Success(new CreateConversationResponse(
                ConversationId: existingConversation.Id,
                WasCreated: false
            ));
        }

        // Create new conversation
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            JobId = request.JobId,
            LastMessageAt = DateTime.UtcNow,
            Participants = new List<ConversationParticipant>
            {
                new ConversationParticipant
                {
                    UserId = userId,
                    JoinedAt = DateTime.UtcNow
                },
                new ConversationParticipant
                {
                    UserId = request.OtherUserId,
                    JoinedAt = DateTime.UtcNow
                }
            }
        };

        // Add initial message if provided
        if (!string.IsNullOrWhiteSpace(request.InitialMessage))
        {
            conversation.Messages = new List<Message>
            {
                new Message
                {
                    Id = Guid.NewGuid(),
                    SenderId = userId,
                    Content = request.InitialMessage,
                    SentAt = DateTime.UtcNow,
                    IsSystemMessage = false
                }
            };
        }

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<CreateConversationResponse>.Success(new CreateConversationResponse(
            ConversationId: conversation.Id,
            WasCreated: true
        ));
    }
}
