using EMPTRACK.Data;
using EMPTRACK.Models;
using EMPTRACK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskEntity = EMPTRACK.Models.Task;
using Task = System.Threading.Tasks.Task;

using EMPTRACK.Services;
namespace EMPTRACK.Controllers;

[Authorize]
public class TasksController : Controller
{
    private static readonly string[] AllowedStatuses =
    {
        "Pending",
        "In Progress",
        "Completed",
        "Cancelled"
    };

    private static readonly string[] AllowedPriorities =
    {
        "Low",
        "Medium",
        "High"
    };

    private readonly ApplicationDbContext _context;

    public TasksController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? assignedEmployeeId,
        string? status,
        string? priority)
    {
        var query = _context.Tasks
            .Include(t => t.AssignedEmployee)
            .Include(t => t.CreatedByEmployee)
            .AsNoTracking()
            .AsQueryable();

        var currentEmployeeId = GetCurrentEmployeeId();

        if (!IsAdmin())
        {
            if (!currentEmployeeId.HasValue)
            {
                return Forbid();
            }

            var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId.Value);

            if (currentEmployee == null)
            {
                return Forbid();
            }

            if (currentEmployee.Role == "Manager" && currentEmployee.TeamId.HasValue)
            {
                var teamId = currentEmployee.TeamId.Value;

                query = query.Where(t =>
                    t.CreatedByEmployeeId == currentEmployeeId.Value ||
                    t.AssignedEmployee.TeamId == teamId);
            }
            else if (currentEmployee.Role == "Head Manager" && currentEmployee.DepartmentId.HasValue)
            {
                var departmentId = currentEmployee.DepartmentId.Value;

                query = query.Where(t =>
                    t.CreatedByEmployeeId == currentEmployeeId.Value ||
                    t.AssignedEmployee.DepartmentId == departmentId);
            }
            else
            {
                query = query.Where(t =>
                    t.AssignedEmployeeId == currentEmployeeId.Value ||
                    t.CreatedByEmployeeId == currentEmployeeId.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(t =>
                t.Title.Contains(search) ||
                t.Description.Contains(search));
        }

        if (assignedEmployeeId.HasValue)
        {
            query = query.Where(t =>
                t.AssignedEmployeeId == assignedEmployeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!AllowedStatuses.Contains(status))
            {
                status = null;
            }
            else
            {
                query = query.Where(t => t.Status == status);
            }
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            if (!AllowedPriorities.Contains(priority))
            {
                priority = null;
            }
            else
            {
                query = query.Where(t => t.Priority == priority);
            }
        }

        ViewData["Search"] = search;
        ViewData["AssignedEmployeeId"] = assignedEmployeeId;
        ViewData["Status"] = status;
        ViewData["Priority"] = priority;

        await PopulateEmployeeListAsync(assignedEmployeeId);

        ViewBag.CanCreate = currentEmployeeId.HasValue;

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenBy(t => t.DueDate)
            .ToListAsync();

        return View(tasks);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var task = await _context.Tasks
            .Include(t => t.AssignedEmployee)
            .Include(t => t.CreatedByEmployee)
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.TaskId == id.Value);

        if (task == null)
        {
            return NotFound();
        }

        if (!await CanViewTaskAsync(task))
        {
            return Forbid();
        }

        var currentEmployeeId = GetCurrentEmployeeId();

        ViewBag.CanEdit =
            currentEmployeeId.HasValue &&
            task.CreatedByEmployeeId == currentEmployeeId.Value;

        ViewBag.CanUpdateStatus =
            currentEmployeeId.HasValue &&
            task.AssignedEmployeeId == currentEmployeeId.Value;

        return View(task);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var employeeId = GetCurrentEmployeeId();

        if (!employeeId.HasValue)
        {
            TempData["ErrorMessage"] =
                "Admin is not linked to an employee, so Admin cannot create a task.";

            return RedirectToAction(nameof(Index));
        }

        await PopulateEmployeeListAsync();

        return View(new TaskCreateViewModel
        {
            DueDate = DateOnly.FromDateTime(DateTime.Today),
            Priority = "Medium"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaskCreateViewModel model)
    {
        var createdByEmployeeId = GetCurrentEmployeeId();

        if (!createdByEmployeeId.HasValue)
        {
            ModelState.AddModelError(
                string.Empty,
                "Your account is not linked to an employee. You cannot create a task.");
        }

        await ValidateTaskInputAsync(
            createdByEmployeeId,
            model.AssignedEmployeeId,
            model.DueDate,
            status: "Pending",
            priority: model.Priority);

        if (!ModelState.IsValid)
        {
            await PopulateEmployeeListAsync(model.AssignedEmployeeId);
            return View(model);
        }

        var task = new TaskEntity
        {
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            AssignedEmployeeId = model.AssignedEmployeeId,
            CreatedByEmployeeId = createdByEmployeeId!.Value,
            DueDate = model.DueDate,
            Status = "Pending",
            Priority = model.Priority,
            CreatedAt = DateTime.Now
        };

        _context.Tasks.Add(task);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The task could not be saved.");

            await PopulateEmployeeListAsync(model.AssignedEmployeeId);
            return View(model);
        }

        TempData["SuccessMessage"] = "Task created successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var task = await _context.Tasks
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.TaskId == id.Value);

        if (task == null)
        {
            return NotFound();
        }

        var currentEmployeeId = GetCurrentEmployeeId();

        if (!currentEmployeeId.HasValue ||
            task.CreatedByEmployeeId != currentEmployeeId.Value)
        {
            return Forbid();
        }

        await PopulateEmployeeListAsync(task.AssignedEmployeeId);

        return View(new TaskEditViewModel
        {
            TaskId = task.TaskId,
            Title = task.Title,
            Description = task.Description,
            AssignedEmployeeId = task.AssignedEmployeeId,
            DueDate = task.DueDate,
            Status = task.Status,
            Priority = task.Priority
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        TaskEditViewModel model)
    {
        if (id != model.TaskId)
        {
            return NotFound();
        }

        var task = await _context.Tasks
            .SingleOrDefaultAsync(t => t.TaskId == id);

        if (task == null)
        {
            return NotFound();
        }

        var currentEmployeeId = GetCurrentEmployeeId();

        if (!currentEmployeeId.HasValue ||
            task.CreatedByEmployeeId != currentEmployeeId.Value)
        {
            return Forbid();
        }

        await ValidateTaskInputAsync(
            currentEmployeeId,
            model.AssignedEmployeeId,
            model.DueDate,
            model.Status,
            model.Priority);

        if (!ModelState.IsValid)
        {
            await PopulateEmployeeListAsync(model.AssignedEmployeeId);
            return View(model);
        }

        task.Title = model.Title.Trim();
        task.Description = model.Description.Trim();
        task.AssignedEmployeeId = model.AssignedEmployeeId;
        task.DueDate = model.DueDate;
        task.Status = model.Status;
        task.Priority = model.Priority;

        // CreatedByEmployeeId and CreatedAt are intentionally unchanged.

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The task could not be updated.");

            await PopulateEmployeeListAsync(model.AssignedEmployeeId);
            return View(model);
        }

        TempData["SuccessMessage"] = "Task updated successfully.";

        return RedirectToAction(nameof(Details), new { id = task.TaskId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string? status)
    {
        var task = await _context.Tasks
            .SingleOrDefaultAsync(t => t.TaskId == id);

        if (task == null)
        {
            return NotFound();
        }

        var currentEmployeeId = GetCurrentEmployeeId();

        if (!currentEmployeeId.HasValue ||
            task.AssignedEmployeeId != currentEmployeeId.Value)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(status) ||
            !AllowedStatuses.Contains(status))
        {
            TempData["ErrorMessage"] = "Please select a valid task status.";
            return RedirectToAction(nameof(Details), new { id });
        }

        task.Status = status;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] = "The task status could not be updated.";
        }

        if (TempData["ErrorMessage"] == null)
        {
            TempData["SuccessMessage"] = "Task status updated successfully.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task ValidateTaskInputAsync(
        int? currentEmployeeId,
        int assignedEmployeeId,
        DateOnly dueDate,
        string? status,
        string? priority)
    {
        if (!currentEmployeeId.HasValue)
        {
            return;
        }

        var assignedEmployee = await _context.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(e =>
                e.EmployeeId == assignedEmployeeId &&
                e.EmploymentStatus == "Active");

        if (assignedEmployee == null)
        {
            ModelState.AddModelError(
                nameof(TaskCreateViewModel.AssignedEmployeeId),
                "The selected employee does not exist or is not active.");
        }
        else
        {
            var creator = await GetCurrentEmployeeAsync(currentEmployeeId.Value);

            if (creator == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The current account is not linked to a valid employee.");
            }
            else if (!CanAssignEmployee(creator, assignedEmployee))
            {
                ModelState.AddModelError(
                    nameof(TaskCreateViewModel.AssignedEmployeeId),
                    "You can only assign tasks within your permitted employee scope.");
            }
        }

        if (dueDate < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(
                nameof(TaskCreateViewModel.DueDate),
                "Due date cannot be in the past.");
        }

        if (string.IsNullOrWhiteSpace(status) ||
            !AllowedStatuses.Contains(status))
        {
            ModelState.AddModelError(
                nameof(TaskEditViewModel.Status),
                "Please select a valid task status.");
        }

        if (string.IsNullOrWhiteSpace(priority) ||
            !AllowedPriorities.Contains(priority))
        {
            ModelState.AddModelError(
                nameof(TaskCreateViewModel.Priority),
                "Please select a valid task priority.");
        }
    }

    private static bool CanAssignEmployee(
        Employee creator,
        Employee assignedEmployee)
    {
        if (creator.Role == "Manager")
        {
            return creator.TeamId.HasValue &&
                   assignedEmployee.TeamId == creator.TeamId.Value;
        }

        if (creator.Role == "Head Manager")
        {
            return creator.DepartmentId.HasValue &&
                   assignedEmployee.DepartmentId == creator.DepartmentId.Value;
        }

        // HR and ordinary employees can create a task for themselves.
        return assignedEmployee.EmployeeId == creator.EmployeeId;
    }

    private async Task<bool> CanViewTaskAsync(TaskEntity task)
    {
        if (IsAdmin())
        {
            return true;
        }

        var currentEmployeeId = GetCurrentEmployeeId();

        if (!currentEmployeeId.HasValue)
        {
            return false;
        }

        if (task.AssignedEmployeeId == currentEmployeeId.Value ||
            task.CreatedByEmployeeId == currentEmployeeId.Value)
        {
            return true;
        }

        var employee = await GetCurrentEmployeeAsync(currentEmployeeId.Value);

        if (employee == null)
        {
            return false;
        }

        if (employee.Role == "Manager" && employee.TeamId.HasValue)
        {
            return task.AssignedEmployee.TeamId == employee.TeamId.Value;
        }

        if (employee.Role == "Head Manager" && employee.DepartmentId.HasValue)
        {
            return task.AssignedEmployee.DepartmentId == employee.DepartmentId.Value;
        }

        return false;
    }

    private async Task<Employee?> GetCurrentEmployeeAsync(int employeeId)
    {
        return await _context.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.EmployeeId == employeeId);
    }

    private async Task PopulateEmployeeListAsync(int? selectedEmployeeId = null)
    {
        var currentEmployeeId = GetCurrentEmployeeId();

        IQueryable<Employee> query = _context.Employees
            .AsNoTracking()
            .Where(e => e.EmploymentStatus == "Active");

        if (currentEmployeeId.HasValue)
        {
            var currentEmployee = await GetCurrentEmployeeAsync(currentEmployeeId.Value);

            if (currentEmployee?.Role == "Manager" &&
                currentEmployee.TeamId.HasValue)
            {
                var teamId = currentEmployee.TeamId.Value;
                query = query.Where(e => e.TeamId == teamId);
            }
            else if (currentEmployee?.Role == "Head Manager" &&
                     currentEmployee.DepartmentId.HasValue)
            {
                var departmentId = currentEmployee.DepartmentId.Value;
                query = query.Where(e => e.DepartmentId == departmentId);
            }
            else
            {
                query = query.Where(e => e.EmployeeId == currentEmployeeId.Value);
            }
        }
        else
        {
            query = query.Where(e => false);
        }

        var employees = await query
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .ToListAsync();

        ViewBag.Employees = employees
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text = $"{e.EmployeeCode} - {e.FirstName} {e.LastName}",
                Selected = selectedEmployeeId == e.EmployeeId
            })
            .ToList();
    }

    private int? GetCurrentEmployeeId()
    {
        var claim = User.FindFirst("EmployeeId");

        if (claim == null ||
            !int.TryParse(claim.Value, out var employeeId))
        {
            return null;
        }

        return employeeId;
    }

    private bool IsAdmin()
    {
        return User.IsInRole("Admin");
    }
}
