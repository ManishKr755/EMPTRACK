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

        ViewBag.Username = User.Identity?.Name ?? "User";

        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
        ViewBag.EmployeeId = employeeIdClaim;

        var isManagementView = IsManagementUser();
        ViewBag.IsManagementView = isManagementView;

        if (isManagementView)
        {
            // Organization-wide metrics are restricted to Admin, HR Head,
            // HR, Head Manager and Manager. A standard Employee never
            // receives this data.
            var totalEmployees = await _context.Employees
                .AsNoTracking()
                .CountAsync();

            var activeEmployees = await _context.Employees
                .AsNoTracking()
                .CountAsync(e => e.EmploymentStatus == "Active");

            var totalDepartments = await _context.Departments
                .AsNoTracking()
                .CountAsync();

            var totalTeams = await _context.Teams
                .AsNoTracking()
                .CountAsync();

            var todayAttendance = await _context.Attendances
                .AsNoTracking()
                .CountAsync(a => a.Date == today);

            var pendingLeaveRequests = await _context.LeaveRequests
                .AsNoTracking()
                .CountAsync(l => l.Status == "Pending");

            var recentEmployees = await _context.Employees
                .AsNoTracking()
                .Include(e => e.Department)
                .Include(e => e.Team)
                .OrderByDescending(e => e.EmployeeId)
                .Take(5)
                .ToListAsync();

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
