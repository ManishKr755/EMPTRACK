namespace EMPTRACK.Models.ViewModels;

public class EmployeeDetailsViewModel
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public int EmployeeSerial { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string Role { get; set; } = string.Empty;

    public string? DepartmentName { get; set; }

    public string? TeamName { get; set; }

    public DateOnly JoiningDate { get; set; }

    public string EmploymentStatus { get; set; } = string.Empty;

    public string? Username { get; set; }
    public string? Password { get; set; }
}