namespace EMPTRACK.Models;

public class Department
{
    public int DepartmentId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int? HeadManagerEmployeeId { get; set; }

    public int? HeadHREmployeeId { get; set; }

    // Navigation properties
    public Employee? HeadManager { get; set; }

    public Employee? HeadHREmployee { get; set; }

    public ICollection<Employee> Employees { get; set; }
        = new List<Employee>();

    public ICollection<Team> Teams { get; set; }
        = new List<Team>();
}