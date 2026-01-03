using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class Conversation : BaseEntity
{
    public Guid JobId { get; set; }
    public DateTime? LastMessageAt { get; set; }

    // Navigation properties
    public Job Job { get; set; } = null!;
    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
