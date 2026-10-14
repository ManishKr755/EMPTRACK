namespace EMPTRACK.Models;

public class Announcement
{
    public int AnnouncementId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; }
}