using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class TaskCreateViewModel
{
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Assigned Employee")]
    public int AssignedEmployeeId { get; set; }

    [Required]
    [Display(Name = "Due Date")]
    public DateOnly DueDate { get; set; }

    [Required]
    [RegularExpression("^(Low|Medium|High)$",
        ErrorMessage = "Priority must be Low, Medium, or High.")]
    public string Priority { get; set; } = "Medium";
}
