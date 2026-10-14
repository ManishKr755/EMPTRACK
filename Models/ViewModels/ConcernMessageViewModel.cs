namespace EMPTRACK.Models.ViewModels;

public class ConcernMessageViewModel
{
    public int MessageId { get; set; }

    public int SenderEmployeeId { get; set; }

    public string SenderName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}