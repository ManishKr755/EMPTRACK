namespace EMPTRACK.Models;

public class Team
{
    public int TeamId { get; set; }

    public int DepartmentId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int ManagerEmployeeId { get; set; }

    // Navigation properties
    public Department Department { get; set; } = null!;

    public Employee ManagerEmployee { get; set; } = null!;

    public ICollection<Employee> Employees { get; set; }
        = new List<Employee>();
}