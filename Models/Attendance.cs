namespace EMPTRACK.Models;

public class Attendance
{
    public int AttendanceId { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly CheckInTime { get; set; }

    public TimeOnly? CheckOutTime { get; set; }

    // Navigation property
    public Employee Employee { get; set; } = null!;
}