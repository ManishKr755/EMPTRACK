namespace EMPTRACK.Models;

public class ConcernMessage
{
    public int MessageId { get; set; }

    public int ConcernId { get; set; }

    public int SenderEmployeeId { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Concern Concern { get; set; } = null!;

    public Employee SenderEmployee { get; set; } = null!;
}