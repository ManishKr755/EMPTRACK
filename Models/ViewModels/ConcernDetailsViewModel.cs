namespace EMPTRACK.Models.ViewModels;

public class ConcernDetailsViewModel
{
    public int ConcernId { get; set; }

    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public string EmployeeCode { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public List<ConcernMessageViewModel> Messages { get; set; }
        = new();

    public bool CanSendMessage { get; set; }
}
