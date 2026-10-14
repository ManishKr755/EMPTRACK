namespace EMPTRACK.Models;

public class Notification
{
    public int NotificationId { get; set; }

    public int EmployeeId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation property
    public Employee Employee { get; set; } = null!;
}