using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class ConcernMessageCreateViewModel
{
    public int ConcernId { get; set; }

    [Required(ErrorMessage = "Message is required.")]
    [StringLength(2000)]
    [Display(Name = "Message")]
    public string Message { get; set; } = string.Empty;
}
