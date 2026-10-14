using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class LeaveRequestCreateViewModel
{
    [Required]
    [Display(Name = "Leave Type")]
    public int LeaveTypeId { get; set; }

    [Required]
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; }

    [Required]
    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; }

    [Required]
    [StringLength(500)]
    [Display(Name = "Reason")]
    public string Reason { get; set; } = string.Empty;
}
