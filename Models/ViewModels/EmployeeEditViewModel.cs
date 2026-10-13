using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class EmployeeEditViewModel
{
    // System-controlled fields.
    // These are displayed but never accepted from the form.
    public int EmployeeId { get; set; }

    public int EmployeeSerial { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    // Login username generated from FirstName + EmployeeCode. Admin-only display.
    public string? Username { get; set; }

    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    [Display(Name = "Team")]
    public int? TeamId { get; set; }

    [Required]
    [Display(Name = "Joining Date")]
    public DateOnly JoiningDate { get; set; }

    [Required]
    [Display(Name = "Employment Status")]
    public string EmploymentStatus { get; set; } = string.Empty;

    // Admin-only password management. CurrentPassword is display-only on GET.
    public string? CurrentPassword { get; set; }

    [DataType(DataType.Password)]
    [StringLength(15, MinimumLength = 8)]
    [Display(Name = "New Password")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword))]
    [Display(Name = "Confirm New Password")]
    public string? ConfirmNewPassword { get; set; }
}