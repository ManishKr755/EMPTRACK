using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class LeaveBalanceCreateViewModel
{
    [Required]
    [Display(Name = "Employee")]
    public int EmployeeId { get; set; }

    [Required]
    [Display(Name = "Leave Type")]
    public int LeaveTypeId { get; set; }
}
