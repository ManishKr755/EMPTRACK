using EMPTRACK.Data;
using EMPTRACK.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EMPTRACK.Controllers;

[Authorize]
public class AttendanceController : Controller
{
    private readonly ApplicationDbContext _context;

    // These are the roles already used by EMPTRACK for administrative/HR access.
    public AttendanceController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Attendance
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        DateOnly? date,
        int? employeeId)
    {
        var isAdministrativeUser = User.IsInRole("Admin") ||
                                    User.IsInRole("HR") ||
                                    User.IsInRole("HR Head") ||
                                    User.IsInRole("Head Manager") ||
                                    User.IsInRole("Manager");

        var query = _context.Attendances
            .Include(a => a.Employee)
            .AsNoTracking()
            .AsQueryable();

        if (User.IsInRole("Manager") || User.IsInRole("Head Manager") || User.IsInRole("HR Head") || User.IsInRole("HR"))
        {
            var currentEmployeeId = GetCurrentEmployeeId();
            var current = currentEmployeeId.HasValue ? await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentEmployeeId.Value) : null;
            if (current == null) return Forbid();
            if (current.Role == "Manager") query = current.TeamId.HasValue ? query.Where(a => a.Employee.TeamId == current.TeamId.Value) : query.Where(a => a.EmployeeId == current.EmployeeId);
            else if (current.Role == "Head Manager") query = current.DepartmentId.HasValue ? query.Where(a => a.Employee.DepartmentId == current.DepartmentId.Value) : query.Where(a => a.EmployeeId == current.EmployeeId);
            else if (current.Role == "HR Head")
            {
                var deptIds = await _context.Departments.AsNoTracking().Where(d => d.DepartmentId == current.DepartmentId || d.HeadHREmployeeId == current.EmployeeId).Select(d => d.DepartmentId).ToListAsync();
                query = query.Where(a => a.Employee.DepartmentId.HasValue && deptIds.Contains(a.Employee.DepartmentId.Value));
            }
            else query = query.Where(a => a.EmployeeId == current.EmployeeId);
            if (employeeId.HasValue) query = query.Where(a => a.EmployeeId == employeeId.Value);
        }

        if (!isAdministrativeUser)
        {
            var currentEmployeeId = GetCurrentEmployeeId();

            if (!currentEmployeeId.HasValue)
            {
                return Forbid();
            }

            // EmployeeId comes from the authenticated user's claim, not the browser.
            query = query.Where(a =>
                a.EmployeeId == currentEmployeeId.Value);

            // Employee users cannot use a browser-supplied employeeId to access another employee.
            employeeId = null;
        }
        else if (employeeId.HasValue)
        {
            query = query.Where(a =>
                a.EmployeeId == employeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(a =>
                a.Employee.EmployeeCode.Contains(search) ||
                a.Employee.FirstName.Contains(search) ||
                a.Employee.LastName.Contains(search));
        }

        if (date.HasValue)
        {
            query = query.Where(a =>
                a.Date == date.Value);
        }

        if (isAdministrativeUser)
        {
            var filterEmployees = _context.Employees.AsNoTracking().AsQueryable();
            if (!User.IsInRole("Admin"))
            {
                var currentId = GetCurrentEmployeeId();
                var current = currentId.HasValue ? await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentId.Value) : null;
                if (current == null) return Forbid();
                if (current.Role == "Manager") filterEmployees = current.TeamId.HasValue ? filterEmployees.Where(e => e.TeamId == current.TeamId.Value) : filterEmployees.Where(e => e.EmployeeId == current.EmployeeId);
                else if (current.Role == "Head Manager") filterEmployees = current.DepartmentId.HasValue ? filterEmployees.Where(e => e.DepartmentId == current.DepartmentId.Value) : filterEmployees.Where(e => e.EmployeeId == current.EmployeeId);
                else if (current.Role == "HR Head")
                {
                    var deptIds = await _context.Departments.AsNoTracking().Where(d => d.DepartmentId == current.DepartmentId || d.HeadHREmployeeId == current.EmployeeId).Select(d => d.DepartmentId).ToListAsync();
                    filterEmployees = filterEmployees.Where(e => e.DepartmentId.HasValue && deptIds.Contains(e.DepartmentId.Value));
                }
                else filterEmployees = filterEmployees.Where(e => e.EmployeeId == current.EmployeeId);
            }
            ViewBag.EmployeeFilter = await filterEmployees.OrderBy(e => e.FirstName).ThenBy(e => e.LastName)
                .Select(e => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = e.EmployeeId.ToString(), Text = e.EmployeeCode + " - " + e.FirstName + " " + e.LastName, Selected = employeeId.HasValue && e.EmployeeId == employeeId.Value }).ToListAsync();
        }

        ViewData["Search"] = search;
        ViewData["Date"] = date;
        ViewData["EmployeeId"] = employeeId;
        ViewData["IsAdministrativeUser"] = isAdministrativeUser;

        var attendanceRecords = await query
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.Employee.FirstName)
            .ThenBy(a => a.Employee.LastName)
            .ToListAsync();

        return View(attendanceRecords);
    }

    // POST: /Attendance/CheckIn
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn()
    {
        if (User.IsInRole("Admin")) return Forbid();
        var employeeId = GetCurrentEmployeeId();

        if (!employeeId.HasValue)
        {
            return Forbid();
        }

        var employee = await _context.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.EmployeeId == employeeId.Value);

        if (employee == null)
        {
            return Forbid();
        }

        if (!string.Equals(
                employee.EmploymentStatus,
                "Active",
                StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] =
                "Attendance cannot be recorded for an inactive employee.";

            return RedirectToAction(nameof(Index));
        }

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        var alreadyExists = await _context.Attendances
            .AnyAsync(a =>
                a.EmployeeId == employeeId.Value &&
                a.Date == today);

        if (alreadyExists)
        {
            TempData["ErrorMessage"] =
                "You have already checked in today.";

            return RedirectToAction(nameof(Index));
        }

        var attendance = new Attendance
        {
            EmployeeId = employeeId.Value,
            Date = today,
            CheckInTime = TimeOnly.FromDateTime(now),
            CheckOutTime = null
        };

        _context.Attendances.Add(attendance);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // The database UNIQUE(EmployeeId, Date) constraint is the final duplicate protection.
            TempData["ErrorMessage"] =
                "You have already checked in today.";

            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] =
            "You have checked in successfully.";

        return RedirectToAction(nameof(Index));
    }

    // POST: /Attendance/CheckOut
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut()
    {
        if (User.IsInRole("Admin")) return Forbid();
        var employeeId = GetCurrentEmployeeId();

        if (!employeeId.HasValue)
        {
            return Forbid();
        }

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        var attendance = await _context.Attendances
            .SingleOrDefaultAsync(a =>
                a.EmployeeId == employeeId.Value &&
                a.Date == today);

        if (attendance == null)
        {
            TempData["ErrorMessage"] =
                "You have not checked in today.";

            return RedirectToAction(nameof(Index));
        }

        if (attendance.CheckOutTime.HasValue)
        {
            TempData["ErrorMessage"] =
                "You have already checked out today.";

            return RedirectToAction(nameof(Index));
        }

        attendance.CheckOutTime = TimeOnly.FromDateTime(now);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "You have checked out successfully.";

        return RedirectToAction(nameof(Index));
    }

    // GET: /Attendance/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var attendance = await _context.Attendances
            .Include(a => a.Employee)
            .AsNoTracking()
            .SingleOrDefaultAsync(a =>
                a.AttendanceId == id.Value);

        if (attendance == null)
        {
            return NotFound();
        }

        var isAdministrativeUser = User.IsInRole("Admin") ||
                                    User.IsInRole("HR") ||
                                    User.IsInRole("HR Head") ||
                                    User.IsInRole("Head Manager") ||
                                    User.IsInRole("Manager");
        if (!User.IsInRole("Admin"))
        {
            var currentEmployeeId = GetCurrentEmployeeId();
            var current = currentEmployeeId.HasValue ? await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentEmployeeId.Value) : null;
            var target = await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == attendance.EmployeeId);
            var allowed = current != null && target != null && (current.EmployeeId == target.EmployeeId ||
                (current.Role == "Manager" && current.TeamId.HasValue && current.TeamId == target.TeamId) ||
                (current.Role == "Head Manager" && current.DepartmentId.HasValue && current.DepartmentId == target.DepartmentId));
            if (!allowed && current != null && current.Role == "HR Head" && target != null && target.DepartmentId.HasValue)
                allowed = await _context.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == target.DepartmentId.Value && d.HeadHREmployeeId == current.EmployeeId);
            if (!allowed) return NotFound();
        }


        if (!isAdministrativeUser)
        {
            var currentEmployeeId = GetCurrentEmployeeId();

            if (!currentEmployeeId.HasValue ||
                attendance.EmployeeId != currentEmployeeId.Value)
            {
                return Forbid();
            }
        }

        ViewData["IsAdministrativeUser"] = isAdministrativeUser;

        return View(attendance);
    }

    private int? GetCurrentEmployeeId()
    {
        var value = User.FindFirstValue("EmployeeId");

        return int.TryParse(value, out var employeeId)
            ? employeeId
            : null;
    }
}
