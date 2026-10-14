using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class LeaveBalanceEditViewModel
{
    public int LeaveBalanceId { get; set; }

    public string EmployeeDisplayName { get; set; } = string.Empty;

    public string LeaveTypeDisplayName { get; set; } = string.Empty;

    [Required]
    [Range(typeof(decimal), "0", "999.99")]
    [Display(Name = "Available Leaves")]
    public decimal AvailableLeaves { get; set; }
}
