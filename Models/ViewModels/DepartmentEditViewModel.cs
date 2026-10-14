using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class DepartmentEditViewModel
{
    [Required]
    [Display(Name = "Department Code")]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Department Name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Head Manager")]
    public int? HeadManagerEmployeeId { get; set; }

    [Display(Name = "HR Head")]
    public int? HeadHREmployeeId { get; set; }
}
