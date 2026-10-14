namespace EMPTRACK.Models;

public class AuditLog
{
    public int AuditLogId { get; set; }

    public int UserAccountId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Target { get; set; } = string.Empty;

    public DateTime DateTime { get; set; }

    public string Description { get; set; } = string.Empty;

    // Navigation property
    public UserAccount UserAccount { get; set; } = null!;
}