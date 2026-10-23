namespace EMPTRACK.Models.ViewModels;

public class EmployeeLeaveBalanceSummaryViewModel
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public decimal TotalAllocated { get; set; }
    public decimal TotalAvailable { get; set; }
    public decimal TotalUsed => Math.Max(0, TotalAllocated - TotalAvailable);
}
