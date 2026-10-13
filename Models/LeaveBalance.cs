namespace EMPTRACK.Models;

public class LeaveBalance
{
    public int LeaveBalanceId { get; set; }

    public int EmployeeId { get; set; }

    public int LeaveTypeId { get; set; }

    public decimal AvailableLeaves { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;

    public LeaveType LeaveType { get; set; } = null!;
}