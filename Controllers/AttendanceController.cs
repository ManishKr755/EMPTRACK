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
            ViewBag.EmployeeFilter = await _context.Employees
                .AsNoTracking()
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .Select(e => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = e.EmployeeId.ToString(),
                    Text = e.EmployeeCode + " - " + e.FirstName + " " + e.LastName,
                    Selected = employeeId.HasValue && e.EmployeeId == employeeId.Value
                })
                .ToListAsync();
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

        if (string.Equals(
                employee.EmploymentStatus,
                "Inactive",
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
