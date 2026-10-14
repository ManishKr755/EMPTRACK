using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class PerformanceReviewCreateViewModel
{
    [Required]
    [Display(Name = "Employee")]
    public int EmployeeId { get; set; }

    [Required]
    [Display(Name = "Review Date")]
    public DateOnly ReviewDate { get; set; }

    [Required]
    [Range(
        1,
        5,
        ErrorMessage = "Rating must be between 1 and 5.")]
    [Display(Name = "Rating")]
    public int Rating { get; set; }

    [Required]
    [StringLength(
        1000,
        MinimumLength = 10,
        ErrorMessage =
            "Feedback must be between 10 and 1000 characters.")]
    public string Feedback { get; set; } = string.Empty;
}