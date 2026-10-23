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

        // Admin, HR and HR Head can view employee concerns.
        // Other users can view only their own concerns.
        if (!CanManageConcerns())
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

        // A normal employee may access only their own concern.
        if (!CanManageConcerns())
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
            CanSendMessage = canSendMessage,
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
            .SingleOrDefaultAsync(c => c.ConcernId == id);

        if (concern == null)
        {
            return NotFound();
        }

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
    [Authorize(Roles = "Admin,HR Head,HR")]
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

        return View(concern);
    }

    // POST: /Concerns/Delete/5
    [HttpPost]
    [Authorize(Roles = "Admin,HR Head,HR")]
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
