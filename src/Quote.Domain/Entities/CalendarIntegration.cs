using Quote.Domain.Common;

namespace Quote.Domain.Entities;

public class CalendarIntegration : BaseAuditableEntity
{
    public Guid TradieProfileId { get; set; }
    public string Provider { get; set; } = "Google";  // Google, Outlook, Apple
    public string EncryptedRefreshToken { get; set; } = string.Empty;
    public string CalendarId { get; set; } = string.Empty;
    public bool SyncEnabled { get; set; } = true;
    public DateTime? LastSyncAt { get; set; }
    public string? LastSyncError { get; set; }

    // Navigation properties
    public TradieProfile TradieProfile { get; set; } = null!;
}
