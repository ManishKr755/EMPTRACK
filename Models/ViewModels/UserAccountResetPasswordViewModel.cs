using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class UserAccountResetPasswordViewModel
{
    public int UserAccountId { get; set; }

    public string Username { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(15, MinimumLength = 8)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("NewPassword")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}