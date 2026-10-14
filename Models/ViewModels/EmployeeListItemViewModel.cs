namespace EMPTRACK.Models.ViewModels;

public class EmployeeListItemViewModel
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string? DepartmentName { get; set; }

    public string? TeamName { get; set; }

    public DateOnly JoiningDate { get; set; }

    public string EmploymentStatus { get; set; } = string.Empty;
}