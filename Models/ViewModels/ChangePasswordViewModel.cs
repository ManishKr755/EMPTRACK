using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class ChangePasswordViewModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(15, MinimumLength = 8)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword))]
    [Display(Name = "Confirm New Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
