namespace EMPTRACK.Models;

public class Holiday
{
    public int HolidayId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateOnly Date { get; set; }
}