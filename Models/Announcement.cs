namespace EMPTRACK.Models;

public class Announcement
{
    public int AnnouncementId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }

    // AllExceptAdmin is the compatibility default for existing announcements.
    public string AudienceType { get; set; } = "AllExceptAdmin";
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
}
