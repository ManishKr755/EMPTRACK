using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class TaskEditViewModel
{
    public int TaskId { get; set; }

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
    [RegularExpression("^(Pending|In Progress|Completed|Cancelled)$",
        ErrorMessage = "Please select a valid task status.")]
    public string Status { get; set; } = "Pending";

    [Required]
    [RegularExpression("^(Low|Medium|High)$",
        ErrorMessage = "Priority must be Low, Medium, or High.")]
    public string Priority { get; set; } = "Medium";
}
