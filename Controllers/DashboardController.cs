using EMPTRACK.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EMPTRACK.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
        var displayName = User.Identity?.Name ?? "User";

        if (int.TryParse(employeeIdClaim, out var linkedEmployeeId))
        {
            var employeeName = await _context.Employees
                .AsNoTracking()
                .Where(e => e.EmployeeId == linkedEmployeeId)
                .Select(e => e.FirstName + " " + e.LastName)
                .SingleOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(employeeName))
            {
                displayName = employeeName;
            }
        }

        ViewBag.Username = displayName;

        var isManagementView = IsManagementUser();
        ViewBag.IsManagementView = isManagementView;

        if (isManagementView)
        {
            // Organization-wide metrics are restricted to Admin, HR Head,
            // HR, Head Manager and Manager. A standard Employee never
            // receives this data.
            var employeesQuery = _context.Employees.AsNoTracking().AsQueryable();
            var teamsQuery = _context.Teams.AsNoTracking().AsQueryable();
            var attendanceQuery = _context.Attendances.AsNoTracking().AsQueryable();
            var leaveQuery = _context.LeaveRequests.AsNoTracking().AsQueryable();
            var departmentCountQuery = _context.Departments.AsNoTracking().AsQueryable();
            var teamCountQuery = _context.Teams.AsNoTracking().AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                if (!int.TryParse(employeeIdClaim, out var scopeEmployeeId)) return Forbid();
                var scopeEmployee = await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == scopeEmployeeId);
                if (scopeEmployee == null) return Forbid();
                if (scopeEmployee.Role == "Manager")
                {
                    if (!scopeEmployee.TeamId.HasValue) return Forbid();
                    var scopeTeamId = scopeEmployee.TeamId.Value;
                    employeesQuery = employeesQuery.Where(e => e.TeamId == scopeTeamId);
                    teamsQuery = teamsQuery.Where(t => t.TeamId == scopeTeamId);
                    attendanceQuery = attendanceQuery.Where(a => a.Employee.TeamId == scopeTeamId);
                    leaveQuery = leaveQuery.Where(l => l.Employee.TeamId == scopeTeamId);
                    departmentCountQuery = departmentCountQuery.Where(d => d.DepartmentId == scopeEmployee.DepartmentId);
                    teamCountQuery = teamCountQuery.Where(t => t.TeamId == scopeTeamId);
                }
                else if (scopeEmployee.Role == "Head Manager")
                {
                    if (!scopeEmployee.DepartmentId.HasValue) return Forbid();
                    var scopeDepartmentId = scopeEmployee.DepartmentId.Value;
                    employeesQuery = employeesQuery.Where(e => e.DepartmentId == scopeDepartmentId);
                    teamsQuery = teamsQuery.Where(t => t.DepartmentId == scopeDepartmentId);
                    attendanceQuery = attendanceQuery.Where(a => a.Employee.DepartmentId == scopeDepartmentId);
                    leaveQuery = leaveQuery.Where(l => l.Employee.DepartmentId == scopeDepartmentId);
                    departmentCountQuery = departmentCountQuery.Where(d => d.DepartmentId == scopeDepartmentId);
                    teamCountQuery = teamCountQuery.Where(t => t.DepartmentId == scopeDepartmentId);
                }
                else if (scopeEmployee.Role == "HR Head")
                {
                    var scopeDepartmentIds = await _context.Departments.AsNoTracking().Where(d => d.DepartmentId == scopeEmployee.DepartmentId || d.HeadHREmployeeId == scopeEmployee.EmployeeId).Select(d => d.DepartmentId).ToListAsync();
                    employeesQuery = employeesQuery.Where(e => e.DepartmentId.HasValue && scopeDepartmentIds.Contains(e.DepartmentId.Value));
                    teamsQuery = teamsQuery.Where(t => scopeDepartmentIds.Contains(t.DepartmentId));
                    attendanceQuery = attendanceQuery.Where(a => a.Employee.DepartmentId.HasValue && scopeDepartmentIds.Contains(a.Employee.DepartmentId.Value));
                    leaveQuery = leaveQuery.Where(l => l.Employee.DepartmentId.HasValue && scopeDepartmentIds.Contains(l.Employee.DepartmentId.Value));
                    departmentCountQuery = departmentCountQuery.Where(d => scopeDepartmentIds.Contains(d.DepartmentId));
                    teamCountQuery = teamCountQuery.Where(t => scopeDepartmentIds.Contains(t.DepartmentId));
                }
                else if (scopeEmployee.Role == "HR")
                {
                    if (!scopeEmployee.DepartmentId.HasValue) return Forbid();
                    var scopeDepartmentId = scopeEmployee.DepartmentId.Value;
                    employeesQuery = employeesQuery.Where(e => e.DepartmentId == scopeDepartmentId);
                    teamsQuery = teamsQuery.Where(t => t.DepartmentId == scopeDepartmentId);
                    attendanceQuery = attendanceQuery.Where(a => a.Employee.DepartmentId == scopeDepartmentId);
                    leaveQuery = leaveQuery.Where(l => l.Employee.DepartmentId == scopeDepartmentId);
                    departmentCountQuery = departmentCountQuery.Where(d => d.DepartmentId == scopeDepartmentId);
                    teamCountQuery = teamCountQuery.Where(t => t.DepartmentId == scopeDepartmentId);
                }
                else return Forbid();
            }

            var totalEmployees = await employeesQuery.CountAsync();
            var activeEmployees = await employeesQuery.CountAsync(e => e.EmploymentStatus == "Active");
            var totalDepartments = await departmentCountQuery.CountAsync();
            var totalTeams = await teamCountQuery.CountAsync();
            var todayAttendance = await attendanceQuery.CountAsync(a => a.Date == today);
            var pendingLeaveRequests = await leaveQuery.CountAsync(l => l.Status == "Pending");
            var recentEmployees = await employeesQuery.Include(e => e.Department).Include(e => e.Team).OrderByDescending(e => e.EmployeeId).Take(5).ToListAsync();

            ViewBag.TotalEmployees = totalEmployees;
            ViewBag.ActiveEmployees = activeEmployees;
            ViewBag.TotalDepartments = totalDepartments;
            ViewBag.TotalTeams = totalTeams;
            ViewBag.TodayAttendance = todayAttendance;
            ViewBag.PendingLeaveRequests = pendingLeaveRequests;
            ViewBag.RecentEmployees = recentEmployees;

            return View();
        }

        // Standard Employee view: personal information only.
        // No organization-wide counts or other employees' records
        // are exposed here.
        if (int.TryParse(employeeIdClaim, out var employeeId))
        {
            var checkedInToday = await _context.Attendances
                .AsNoTracking()
                .AnyAsync(a => a.EmployeeId == employeeId && a.Date == today);

            var myPendingLeaveRequests = await _context.LeaveRequests
                .AsNoTracking()
                .CountAsync(l =>
                    l.EmployeeId == employeeId &&
                    l.Status == "Pending");

            var myUnreadNotifications = await _context.Notifications
                .AsNoTracking()
                .CountAsync(n =>
                    n.EmployeeId == employeeId &&
                    !n.IsRead);

            ViewBag.CheckedInToday = checkedInToday;
            ViewBag.MyPendingLeaveRequests = myPendingLeaveRequests;
            ViewBag.MyUnreadNotifications = myUnreadNotifications;
        }

        return View();
    }

    private bool IsManagementUser()
    {
        return User.IsInRole("Admin") ||
               User.IsInRole("HR Head") ||
               User.IsInRole("HR") ||
               User.IsInRole("Head Manager") ||
               User.IsInRole("Manager");
    }
}
