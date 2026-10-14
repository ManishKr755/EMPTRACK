namespace EMPTRACK.Models;

public class PerformanceReview
{
    public int ReviewId { get; set; }

    public int EmployeeId { get; set; }

    public int ReviewerEmployeeId { get; set; }

    public DateOnly ReviewDate { get; set; }

    public int Rating { get; set; }

    public string Feedback { get; set; } = string.Empty;

    // Navigation properties
    public Employee Employee { get; set; } = null!;

    public Employee Reviewer { get; set; } = null!;
}