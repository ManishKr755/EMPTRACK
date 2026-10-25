using EMPTRACK.Data;
using EMPTRACK.Models;
using EMPTRACK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Task = System.Threading.Tasks.Task;

using EMPTRACK.Services;
namespace EMPTRACK.Controllers;

[Authorize]
public class ConcernsController : Controller
{
    private static readonly string[] ValidStatuses =
    {
        "Open",
        "In Progress",
        "Resolved",
        "Closed"
    };

    private readonly ApplicationDbContext _context;

    public ConcernsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Concerns
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? status)
    {
        var query = _context.Concerns
            .AsNoTracking()
            .Include(c => c.Employee)
            .AsQueryable();

        // Apply department/team scope for supervisory roles before any filters.
        var currentIdForScope = GetCurrentEmployeeId();
        if (!User.IsInRole("Admin") && currentIdForScope.HasValue && (User.IsInRole("Head Manager") || User.IsInRole("Manager") || User.IsInRole("HR Head") || User.IsInRole("HR")))
        {
            var current = await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentIdForScope.Value);
            if (current == null) return Forbid();
            if (current.Role == "Manager") query = current.TeamId.HasValue ? query.Where(c => c.Employee.TeamId == current.TeamId.Value) : query.Where(c => false);
            else if (current.Role == "Head Manager") query = current.DepartmentId.HasValue ? query.Where(c => c.Employee.DepartmentId == current.DepartmentId.Value) : query.Where(c => false);
            else if (current.Role == "HR") query = current.DepartmentId.HasValue ? query.Where(c => c.Employee.DepartmentId == current.DepartmentId.Value) : query.Where(c => c.EmployeeId == current.EmployeeId);
            else if (current.Role == "HR Head")
            {
                var departmentIds = await _context.Departments.AsNoTracking().Where(d => d.DepartmentId == current.DepartmentId || d.HeadHREmployeeId == current.EmployeeId).Select(d => d.DepartmentId).ToListAsync();
                query = query.Where(c => c.Employee.DepartmentId.HasValue && departmentIds.Contains(c.Employee.DepartmentId.Value));
            }
        }

        // Admin, HR and HR Head can view employee concerns; other users are scoped above or to their own.
        if (!CanManageConcerns() && !User.IsInRole("Head Manager") && !User.IsInRole("Manager"))
        {
            var employeeId = GetCurrentEmployeeId();

            if (!employeeId.HasValue)
            {
                return Forbid();
            }

            query = query.Where(c => c.EmployeeId == employeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            if (CanManageConcerns())
            {
                query = query.Where(c =>
                    c.Subject.Contains(search) ||
                    c.Employee.FirstName.Contains(search) ||
                    c.Employee.LastName.Contains(search) ||
                    c.Employee.EmployeeCode.Contains(search));
            }
            else
            {
                query = query.Where(c =>
                    c.Subject.Contains(search));
            }
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            status = status.Trim();

            if (!ValidStatuses.Contains(status))
            {
                status = null;
            }
            else
            {
                query = query.Where(c => c.Status == status);
            }
        }

        ViewData["Search"] = search;
        ViewData["Status"] = status;

        var concerns = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.ConcernId)
            .ToListAsync();

        return View(concerns);
    }

    // GET: /Concerns/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var concern = await _context.Concerns
            .AsNoTracking()
            .Include(c => c.Employee)
            .Include(c => c.Messages)
                .ThenInclude(m => m.SenderEmployee)
            .SingleOrDefaultAsync(c =>
                c.ConcernId == id.Value);

        if (concern == null)
        {
            return NotFound();
        }

        if (!User.IsInRole("Admin"))
        {
            var raw = User.FindFirst("EmployeeId")?.Value;
            var currentId = int.TryParse(raw, out var parsed) ? parsed : 0;
            var current = await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentId);
            var permitted = current != null && (concern.EmployeeId == current.EmployeeId ||
                (current.Role == "Manager" && current.TeamId.HasValue && concern.Employee.TeamId == current.TeamId) ||
                ((current.Role == "Head Manager" || current.Role == "HR") && current.DepartmentId.HasValue && concern.Employee.DepartmentId == current.DepartmentId));
            if (!permitted && current?.Role == "HR Head" && concern.Employee.DepartmentId.HasValue)
                permitted = current.DepartmentId == concern.Employee.DepartmentId || await _context.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == concern.Employee.DepartmentId.Value && d.HeadHREmployeeId == current.EmployeeId);
            if (!permitted) return NotFound();
        }

        // A normal employee may access only their own concern.
        if (!CanManageConcerns() && !User.IsInRole("Head Manager") && !User.IsInRole("Manager"))
        {
            var employeeId = GetCurrentEmployeeId();

            if (!employeeId.HasValue ||
                concern.EmployeeId != employeeId.Value)
            {
                return Forbid();
            }
        }

        var currentEmployeeId = GetCurrentEmployeeId();

        var canSendMessage =
            currentEmployeeId.HasValue &&
            (concern.EmployeeId == currentEmployeeId.Value ||
             User.IsInRole("HR") ||
             User.IsInRole("HR Head"));

        var model = new ConcernDetailsViewModel
        {
            ConcernId = concern.ConcernId,
            EmployeeId = concern.EmployeeId,
            EmployeeName =
                $"{concern.Employee.FirstName} {concern.Employee.LastName}",
            EmployeeCode = concern.Employee.EmployeeCode,
            Subject = concern.Subject,
            Status = concern.Status,
            CreatedAt = concern.CreatedAt,
            CanSendMessage = false,
            Messages = concern.Messages
                .OrderBy(m => m.CreatedAt)
                .ThenBy(m => m.MessageId)
                .Select(m => new ConcernMessageViewModel
                {
                    MessageId = m.MessageId,
                    SenderEmployeeId = m.SenderEmployeeId,
                    SenderName =
                        $"{m.SenderEmployee.FirstName} {m.SenderEmployee.LastName}",
                    Message = m.Message,
                    CreatedAt = m.CreatedAt
                })
                .ToList()
        };

        return View(model);
    }

    // GET: /Concerns/Create
    [HttpGet]
    public IActionResult Create()
    {
        if (User.IsInRole("Admin")) return Forbid();
        if (!GetCurrentEmployeeId().HasValue)
        {
            return Forbid();
        }

        return View(new ConcernCreateViewModel());
    }

    // POST: /Concerns/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ConcernCreateViewModel model)
    {
        if (User.IsInRole("Admin")) return Forbid();
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var employeeId = GetCurrentEmployeeId();

        if (!employeeId.HasValue)
        {
            return Forbid();
        }

        var employeeExists = await _context.Employees
            .AsNoTracking()
            .AnyAsync(e => e.EmployeeId == employeeId.Value);

        if (!employeeExists)
        {
            return Forbid();
        }

        var subject = model.Subject.Trim();

        if (string.IsNullOrWhiteSpace(subject))
        {
            ModelState.AddModelError(
                nameof(model.Subject),
                "Subject is required.");

            return View(model);
        }

        var concern = new Concern
        {
            // EmployeeId comes from the authenticated user.
            EmployeeId = employeeId.Value,
            Subject = subject,
            Status = "Open",
            CreatedAt = DateTime.Now
        };

        _context.Concerns.Add(concern);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The concern could not be created.");

            return View(model);
        }

        TempData["SuccessMessage"] =
            "Concern created successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id = concern.ConcernId });
    }

    // GET: /Concerns/Edit/5 - only the concern creator may edit content.
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (User.IsInRole("Admin")) return Forbid();
        if (!id.HasValue) return NotFound();
        var concern = await _context.Concerns.AsNoTracking().SingleOrDefaultAsync(c => c.ConcernId == id.Value);
        if (concern == null) return NotFound();
        if (concern.EmployeeId != GetCurrentEmployeeId()) return Forbid();
        if (concern.Status == "Closed") return Forbid();
        return View(new ConcernCreateViewModel { Subject = concern.Subject });
    }

    // POST: /Concerns/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ConcernCreateViewModel model)
    {
        if (User.IsInRole("Admin")) return Forbid();
        var concern = await _context.Concerns.SingleOrDefaultAsync(c => c.ConcernId == id);
        if (concern == null) return NotFound();
        if (concern.EmployeeId != GetCurrentEmployeeId()) return Forbid();
        if (concern.Status == "Closed") return Forbid();
        if (!ModelState.IsValid) return View(model);
        var subject = model.Subject.Trim();
        if (string.IsNullOrWhiteSpace(subject)) { ModelState.AddModelError(nameof(model.Subject), "Subject is required."); return View(model); }
        concern.Subject = subject;
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateException) { ModelState.AddModelError(string.Empty, "The concern could not be updated."); return View(model); }
        TempData["SuccessMessage"] = "Concern updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Concerns/ChangeStatus
    [HttpPost]
    [Authorize(Roles = "Admin,HR Head,HR")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(
        int id,
        string status)
    {
        if (!ValidStatuses.Contains(status))
        {
            TempData["ErrorMessage"] =
                "Invalid concern status.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        var concern = await _context.Concerns
            .Include(c => c.Employee)
            .SingleOrDefaultAsync(c => c.ConcernId == id);

        if (concern == null)
        {
            return NotFound();
        }
        if (!await CanManageConcernStatusAsync(concern)) return Forbid();

        // Keep the finalized status flow simple:
        // Open -> In Progress -> Resolved -> Closed.
        if (!IsAllowedNextStatus(concern.Status, status))
        {
            TempData["ErrorMessage"] =
                "The selected status is not a valid next status.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        concern.Status = status;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                "The concern status could not be updated.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        TempData["SuccessMessage"] =
            "Concern status updated successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id });
    }

    // GET: /Concerns/Delete/5
    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var concern = await _context.Concerns
            .AsNoTracking()
            .Include(c => c.Employee)
            .SingleOrDefaultAsync(c => c.ConcernId == id.Value);

        if (concern == null)
        {
            return NotFound();
        }
        if (!User.IsInRole("Admin") && concern.EmployeeId != GetCurrentEmployeeId()) return Forbid();
        if (await _context.ConcernMessages.AnyAsync(m => m.ConcernId == concern.ConcernId))
        {
            TempData["ErrorMessage"] = "This concern has historical messages and cannot be deleted without losing history.";
            return RedirectToAction(nameof(Details), new { id = concern.ConcernId });
        }

        return View(concern);
    }

    // POST: /Concerns/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var concern = await _context.Concerns
            .Include(c => c.Messages)
            .SingleOrDefaultAsync(c => c.ConcernId == id);

        if (concern == null)
        {
            return NotFound();
        }
        if (!User.IsInRole("Admin") && concern.EmployeeId != GetCurrentEmployeeId()) return Forbid();
        if (await _context.ConcernMessages.AnyAsync(m => m.ConcernId == concern.ConcernId))
        {
            TempData["ErrorMessage"] = "This concern has historical messages and cannot be deleted without losing history.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ConcernMessage is a separate upcoming module, but existing
        // messages must be removed with their parent to satisfy the
        // existing required foreign key when a concern is deleted.
        _context.Concerns.Remove(concern);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                "The concern could not be deleted.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        TempData["SuccessMessage"] =
            "Concern deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> CanManageConcernStatusAsync(Concern concern)
    {
        if (User.IsInRole("Admin")) return true;
        var currentId = GetCurrentEmployeeId();
        if (!currentId.HasValue) return false;
        var current = await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentId.Value);
        if (current == null || concern.Employee == null) return false;
        if (current.Role == "HR") return current.DepartmentId.HasValue && current.DepartmentId == concern.Employee.DepartmentId;
        if (current.Role == "HR Head") return current.DepartmentId == concern.Employee.DepartmentId || await _context.Departments.AnyAsync(d => d.DepartmentId == concern.Employee.DepartmentId && d.HeadHREmployeeId == current.EmployeeId);
        return false;
    }

    private bool CanManageConcerns()
    {
        return User.IsInRole("Admin") ||
               User.IsInRole("HR") ||
               User.IsInRole("HR Head");
    }

    private static bool IsAllowedNextStatus(
        string currentStatus,
        string newStatus)
    {
        return currentStatus switch
        {
            "Open" => newStatus == "In Progress",
            "In Progress" => newStatus == "Resolved",
            "Resolved" => newStatus == "Closed",
            "Closed" => false,
            _ => false
        };
    }

    private int? GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId");

        if (claim == null)
        {
            return null;
        }

        return int.TryParse(
            claim.Value,
            out var employeeId)
            ? employeeId
            : null;
    }
}
