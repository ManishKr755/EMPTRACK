using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class DepartmentCreateViewModel
{
    [Required]
    [Display(Name = "Department Code")]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Department Name")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}
