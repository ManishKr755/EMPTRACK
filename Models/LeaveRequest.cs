namespace EMPTRACK.Models;

public class LeaveRequest
{
    public int LeaveRequestId { get; set; }

    public int EmployeeId { get; set; }

    public int LeaveTypeId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int? ApprovedByUserId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;

    public LeaveType LeaveType { get; set; } = null!;

    public UserAccount? ApprovedByUser { get; set; }
}