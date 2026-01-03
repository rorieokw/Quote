using Quote.Domain.Common;
using Quote.Domain.Enums;

namespace Quote.Domain.Entities;

public class TeamMember : BaseAuditableEntity
{
    public Guid TradieProfileId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public TeamMemberRole Role { get; set; }
    public decimal? HourlyRate { get; set; }
    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}";

    // Navigation properties
    public TradieProfile TradieProfile { get; set; } = null!;
    public ICollection<JobAssignment> Assignments { get; set; } = new List<JobAssignment>();
}
