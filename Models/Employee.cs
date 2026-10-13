namespace EMPTRACK.Models;

public class Employee
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public int EmployeeSerial { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string Role { get; set; } = string.Empty;

    public int? TeamId { get; set; }

    public int? DepartmentId { get; set; }

    public DateOnly JoiningDate { get; set; }

    public string EmploymentStatus { get; set; } = string.Empty;

    // Navigation properties
    public Team? Team { get; set; }

    public Department? Department { get; set; }

    public ICollection<UserAccount> UserAccounts { get; set; }
        = new List<UserAccount>();

    public ICollection<Attendance> Attendances { get; set; }
        = new List<Attendance>();

    public ICollection<LeaveBalance> LeaveBalances { get; set; }
        = new List<LeaveBalance>();

    public ICollection<LeaveRequest> LeaveRequests { get; set; }
        = new List<LeaveRequest>();

    public ICollection<EMPTRACK.Models.Task> AssignedTasks { get; set; }
        = new List<EMPTRACK.Models.Task>();

    public ICollection<EMPTRACK.Models.Task> CreatedTasks { get; set; }
        = new List<EMPTRACK.Models.Task>();

    public ICollection<PerformanceReview> Reviews { get; set; }
        = new List<PerformanceReview>();

    public ICollection<PerformanceReview> ReviewsGiven { get; set; }
        = new List<PerformanceReview>();

    public ICollection<SalaryPayment> SalaryPayments { get; set; }
        = new List<SalaryPayment>();

    public ICollection<Notification> Notifications { get; set; }
        = new List<Notification>();

    public ICollection<Concern> Concerns { get; set; }
        = new List<Concern>();

    public ICollection<ConcernMessage> ConcernMessages { get; set; }
        = new List<ConcernMessage>();
}