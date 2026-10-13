namespace EMPTRACK.Models;

public class LeaveType
{
    public int LeaveTypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string AllocationType { get; set; } = "Annually";

    public decimal AllocationLimit { get; set; }

    public bool IsActive { get; set; }

    public ICollection<LeaveBalance> LeaveBalances { get; set; }
        = new List<LeaveBalance>();

    public ICollection<LeaveRequest> LeaveRequests { get; set; }
        = new List<LeaveRequest>();
}
