namespace EMPTRACK.Models;

public class Task
{
    public int TaskId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int AssignedEmployeeId { get; set; }

    public int CreatedByEmployeeId { get; set; }

    public DateOnly DueDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Employee AssignedEmployee { get; set; } = null!;

    public Employee CreatedByEmployee { get; set; } = null!;
}