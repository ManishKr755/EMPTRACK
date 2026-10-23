namespace EMPTRACK.Models;

public class LeaveBalance
{
    public int LeaveBalanceId { get; set; }

    public int EmployeeId { get; set; }

    public int LeaveTypeId { get; set; }

    public decimal AvailableLeaves { get; set; }

    // First day of the most recently granted monthly/annual allocation period.
    public DateOnly? LastAccruedPeriod { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;

    public LeaveType LeaveType { get; set; } = null!;
}