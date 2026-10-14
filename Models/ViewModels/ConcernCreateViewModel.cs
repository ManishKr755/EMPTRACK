using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class ConcernCreateViewModel
{
    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
    public string Subject { get; set; } = string.Empty;
}
