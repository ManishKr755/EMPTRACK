namespace EMPTRACK.Models.ViewModels;

public class EmployeeLeaveBalanceDetailViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public decimal TotalAllocated { get; set; }
    public decimal TotalAvailable { get; set; }
    public List<LeaveTypeBalanceRowViewModel> LeaveTypes { get; set; } = new();
}

public class LeaveTypeBalanceRowViewModel
{
    public int LeaveTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AllocationType { get; set; } = string.Empty;
    public decimal Allocated { get; set; }
    public decimal Available { get; set; }
    public decimal Used { get; set; }
    public bool IsActive { get; set; }
}
