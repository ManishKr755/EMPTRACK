using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class AttendanceEditViewModel
{
    public int AttendanceId { get; set; }

    [Required]
    [Display(Name = "Employee")]
    public int EmployeeId { get; set; }

    [Required]
    [Display(Name = "Date")]
    public DateOnly Date { get; set; }

    [Required]
    [Display(Name = "Check-in Time")]
    public TimeOnly CheckInTime { get; set; }

    [Display(Name = "Check-out Time")]
    public TimeOnly? CheckOutTime { get; set; }
}