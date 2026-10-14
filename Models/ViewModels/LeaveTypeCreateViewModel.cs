using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class LeaveTypeCreateViewModel
{
    [Required]
    [StringLength(100)]
    [Display(Name = "Leave Type Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Annually|Monthly)$", ErrorMessage = "Select Annually or Monthly.")]
    [Display(Name = "Allocation Type")]
    public string AllocationType { get; set; } = "Annually";

    [Required]
    [Range(typeof(decimal), "0.01", "999.99")]
    [Display(Name = "Number of Leaves")]
    public decimal AllocationLimit { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
