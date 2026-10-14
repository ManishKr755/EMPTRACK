using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class EmployeeCreateViewModel
{
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

    [Required]
    [DataType(DataType.Password)]
    [StringLength(15, MinimumLength = 8)]
    [Display(Name = "Initial Password")]
    public string InitialPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(InitialPassword))]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}