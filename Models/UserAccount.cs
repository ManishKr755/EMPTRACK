namespace EMPTRACK.Models;

public class UserAccount
{
    public int UserAccountId { get; set; }

    public int? EmployeeId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    // Reversibly protected copy used only by the single Admin for demo-account management.
    public string? EncryptedPassword { get; set; }

    public bool IsActive { get; set; }

    // Navigation properties
    public Employee? Employee { get; set; }

    public ICollection<LeaveRequest> ApprovedLeaveRequests { get; set; }
        = new List<LeaveRequest>();

    public ICollection<AuditLog> AuditLogs { get; set; }
        = new List<AuditLog>();
}