using EMPTRACK.Models;

namespace EMPTRACK.Models.ViewModels;

public class EmployeeIndexViewModel
{
    public string? Search { get; set; }

    public int? DepartmentId { get; set; }

    public string? Role { get; set; }

    public string? EmploymentStatus { get; set; }

    public List<EmployeeListItemViewModel> Employees { get; set; }
        = new List<EmployeeListItemViewModel>();

    public List<Department> Departments { get; set; }
        = new List<Department>();

    public List<string> Roles { get; set; }
        = new List<string>();

    public List<string> EmploymentStatuses { get; set; }
        = new List<string>();
}