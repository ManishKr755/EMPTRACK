using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class HolidayCreateViewModel
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Holiday Date")]
    public DateOnly Date { get; set; }
}