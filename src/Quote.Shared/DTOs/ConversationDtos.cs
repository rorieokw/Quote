namespace Quote.Shared.DTOs;

// List view of conversations (inbox)
public record ConversationDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string OtherPartyName,
    string? OtherPartyAvatar,
    string? LastMessage,
    DateTime? LastMessageAt,
    int UnreadCount,
    bool IsOnline
);

// Single message in a conversation
public record MessageDto(
    Guid Id,
    Guid SenderId,
    string SenderName,
    string Content,
    string? MediaUrl,
    string? MediaType,      // "image", "file"
    string? FileName,
    DateTime SentAt,
    DateTime? ReadAt,
    bool IsSystemMessage,
    bool IsMine
);

// Conversation detail with messages
public record ConversationDetailDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string JobStatus,
    ParticipantDto OtherParty,
    List<MessageDto> Messages,
    int TotalMessages
);

// Participant info
public record ParticipantDto(
    Guid UserId,
    string Name,
    string? AvatarUrl,
    string UserType,
    DateTime? LastReadAt
);

// Request to send a message
public record SendMessageRequest(
    Guid ConversationId,
    string Content,
    string? MediaUrl,
    string? MediaType,
    string? FileName
);

// Request to create or get conversation
public record CreateConversationRequest(
    Guid JobId,
    Guid OtherUserId,
    string? InitialMessage
);

// Response for unread count (for nav badge)
public record UnreadCountDto(
    int TotalUnread
);

// Paginated conversations list
public record ConversationListResponse(
    List<ConversationDto> Conversations,
    int TotalCount,
    int Page,
    int PageSize
);

// Paginated messages list
public record MessageListResponse(
    List<MessageDto> Messages,
    int TotalCount,
    int Page,
    int PageSize,
    bool HasMore
);
