using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class UserAccountEditViewModel
{
    public int UserAccountId { get; set; }

    [Required]
    [Display(Name = "Employee")]
    public int? EmployeeId { get; set; }

    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; }
}