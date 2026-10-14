using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class TeamEditViewModel
{
    public int TeamId { get; set; }

    [Required]
    [Display(Name = "Department")]
    public int DepartmentId { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Team Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Manager")]
    public int ManagerEmployeeId { get; set; }
}