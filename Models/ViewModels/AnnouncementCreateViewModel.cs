using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class AnnouncementCreateViewModel
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;
}
