using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class NotificationCreateViewModel
{
    [Required]
    [Display(Name = "Employee")]
    public int EmployeeId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;
}
