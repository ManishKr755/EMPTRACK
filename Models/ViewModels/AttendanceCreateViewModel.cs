using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class AttendanceCreateViewModel
{
    [Required]
    [Display(Name = "Employee")]
    public int EmployeeId { get; set; }

    [Required]
    [Display(Name = "Date")]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    [Display(Name = "Check-in Time")]
    public TimeOnly CheckInTime { get; set; } =
        TimeOnly.FromDateTime(DateTime.Now);

    [Display(Name = "Check-out Time")]
    public TimeOnly? CheckOutTime { get; set; }
}