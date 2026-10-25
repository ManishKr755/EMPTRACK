using EMPTRACK.Data;
using EMPTRACK.Models;
using EMPTRACK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

using EMPTRACK.Services;
namespace EMPTRACK.Controllers;

[Authorize(Roles = "Admin,HR Head,Head Manager")]
public class DepartmentsController : Controller
{
    // The finalized project/test data identifies HR with code 01.
    // Keep this rule in one place instead of scattering "01" through the controller.
    private const string HrDepartmentCode = "01";

    private readonly ApplicationDbContext _context;

    public DepartmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Departments
    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Departments
            .Include(d => d.HeadManager)
            .Include(d => d.HeadHREmployee)
            .Include(d => d.Employees)
            .Include(d => d.Teams)
            .AsNoTracking();

        if (!User.IsInRole("Admin"))
        {
            var raw = User.FindFirst("EmployeeId")?.Value;
            var currentId = int.TryParse(raw, out var parsed) ? parsed : 0;
            var current = await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentId);
            if (current == null) return Forbid();
            if (current.Role == "Head Manager") query = query.Where(d => d.DepartmentId == current.DepartmentId);
            else if (current.Role == "HR Head") query = query.Where(d => d.HeadHREmployeeId == current.EmployeeId || d.DepartmentId == current.DepartmentId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(d =>
                d.Code.Contains(search) ||
                d.Name.Contains(search));
        }

        ViewData["Search"] = search;

        var departments = await query
            .OrderBy(d => d.Code)
            .ToListAsync();

        return View(departments);
    }

    // GET: /Departments/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
            return NotFound();

        var department = await _context.Departments
            .Include(d => d.HeadManager)
            .Include(d => d.HeadHREmployee)
            .Include(d => d.Employees)
            .Include(d => d.Teams)
                .ThenInclude(t => t.ManagerEmployee)
            .AsNoTracking()
            .SingleOrDefaultAsync(d => d.DepartmentId == id.Value);

        if (department == null)
            return NotFound();
        if (!User.IsInRole("Admin"))
        {
            var raw = User.FindFirst("EmployeeId")?.Value;
            var currentId = int.TryParse(raw, out var parsed) ? parsed : 0;
            var current = await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentId);
            var permitted = current != null && ((current.Role == "Head Manager" && current.DepartmentId == department.DepartmentId) || (current.Role == "HR Head" && (current.DepartmentId == department.DepartmentId || department.HeadHREmployeeId == current.EmployeeId)));
            if (!permitted) return NotFound();
        }

        return View(department);
    }

    // Department creation is restricted to Admin only.
    // GET: /Departments/Create
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Create()
    {
        // Leadership is assigned after the department exists.
        // This avoids a circular dependency during department creation.
        return View(new DepartmentCreateViewModel());
    }

    // POST: /Departments/Create
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentCreateViewModel model)
    {
        model.Code = model.Code?.Trim() ?? string.Empty;
        model.Name = model.Name?.Trim() ?? string.Empty;

        ValidateCodeFormat(model.Code);

        if (await _context.Departments.AnyAsync(d => d.Code == model.Code))
        {
            ModelState.AddModelError(
                nameof(model.Code),
                "A department with this code already exists.");
        }

        if (await _context.Departments.AnyAsync(d =>
                d.Name.ToUpper() == model.Name.ToUpper()))
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A department with this name already exists.");
        }

        if (!ModelState.IsValid)
            return View(model);

        var department = new Department
        {
            Code = model.Code,
            Name = model.Name,
            HeadManagerEmployeeId = null,
            HeadHREmployeeId = null
        };

        _context.Departments.Add(department);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The department could not be saved. The department code or name may already exist.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Department created successfully. You can assign department heads from Edit.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Departments/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
            return NotFound();

        var department = await _context.Departments
            .AsNoTracking()
            .SingleOrDefaultAsync(d => d.DepartmentId == id.Value);

        if (department == null)
            return NotFound();

        if (!await CanEditDepartmentAsync(department))
            return Forbid();

        var isHrDepartment = IsHRDepartment(department.Code);
        var model = new DepartmentEditViewModel
        {
            Code = department.Code,
            Name = department.Name,
            HeadEmployeeId = isHrDepartment
                ? department.HeadHREmployeeId
                : department.HeadManagerEmployeeId
        };

        await PopulateDepartmentHeadListAsync(department, model.HeadEmployeeId);
        return View(model);
    }

    // POST: /Departments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DepartmentEditViewModel model)
    {
        var department = await _context.Departments
            .SingleOrDefaultAsync(d => d.DepartmentId == id);

        if (department == null)
            return NotFound();

        if (!await CanEditDepartmentAsync(department))
            return Forbid();

        model.Code = model.Code?.Trim() ?? string.Empty;
        model.Name = model.Name?.Trim() ?? string.Empty;

        ValidateCodeFormat(model.Code);

        // The department type is defined by its code. If the code is changed
        // while the department has no employees, use the submitted code.
        var isHrDepartment = IsHRDepartment(model.Code);

        if (!string.Equals(department.Code, model.Code, StringComparison.Ordinal))
        {
            var hasEmployees = await _context.Employees
                .AnyAsync(e => e.DepartmentId == department.DepartmentId);

            if (hasEmployees)
            {
                ModelState.AddModelError(
                    nameof(model.Code),
                    "Department code cannot be changed while employees are assigned to this department.");
            }
        }

        if (await _context.Departments.AnyAsync(d =>
                d.DepartmentId != id && d.Code == model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "A department with this code already exists.");
        }

        if (await _context.Departments.AnyAsync(d =>
                d.DepartmentId != id && d.Name.ToUpper() == model.Name.ToUpper()))
        {
            ModelState.AddModelError(nameof(model.Name), "A department with this name already exists.");
        }

        if (model.HeadEmployeeId.HasValue)
        {
            var head = await _context.Employees
                .AsNoTracking()
                .SingleOrDefaultAsync(e => e.EmployeeId == model.HeadEmployeeId.Value);

            var requiredRole = isHrDepartment ? "HR Head" : "Head Manager";

            if (head == null)
            {
                ModelState.AddModelError(nameof(model.HeadEmployeeId), "The selected department head does not exist.");
            }
            else
            {
                if (!string.Equals(head.Role, requiredRole, StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(nameof(model.HeadEmployeeId), $"The selected employee must have the {requiredRole} role.");
                }

                if (head.DepartmentId != department.DepartmentId)
                {
                    ModelState.AddModelError(nameof(model.HeadEmployeeId), "The department head must belong to this department.");
                }

                if (!string.Equals(head.EmploymentStatus, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(nameof(model.HeadEmployeeId), "The department head must be an active employee.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateDepartmentHeadListAsync(department, model.HeadEmployeeId);
            return View(model);
        }

        department.Code = model.Code;
        department.Name = model.Name;

        // Keep the existing database relationships; the UI exposes only one
        // Head field and maps it to the correct relationship for this department.
        if (isHrDepartment)
        {
            department.HeadHREmployeeId = model.HeadEmployeeId;
            department.HeadManagerEmployeeId = null;
        }
        else
        {
            department.HeadManagerEmployeeId = model.HeadEmployeeId;
            department.HeadHREmployeeId = null;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The department could not be updated. The department code or name may already exist.");
            await PopulateDepartmentHeadListAsync(department, model.HeadEmployeeId);
            return View(model);
        }

        TempData["SuccessMessage"] = "Department updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<bool> CanEditDepartmentAsync(Department department)
    {
        // Admin can manage department details. HR Head and Head Manager are
        // limited to the department they lead, with HR Head restricted to HR.
        if (User.IsInRole("Admin"))
            return true;

        var employeeIdValue = User.FindFirst("EmployeeId")?.Value;
        if (!int.TryParse(employeeIdValue, out var employeeId))
            return false;

        var employee = await _context.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.EmployeeId == employeeId);

        if (employee == null)
            return false;

        if (User.IsInRole("HR Head"))
            return IsHRDepartment(department.Code) &&
                   employee.Role == "HR Head" &&
                   employee.DepartmentId == department.DepartmentId;

        if (User.IsInRole("Head Manager"))
            return !IsHRDepartment(department.Code) &&
                   employee.Role == "Head Manager" &&
                   employee.DepartmentId == department.DepartmentId;

        return false;
    }

    private void ValidateCodeFormat(string code)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(code, "^\\d{2}$"))
        {
            ModelState.AddModelError(
                "Code",
                "Department code must contain exactly two digits, for example 01 or 02.");
        }
    }

    private static bool IsHRDepartment(string? code)
    {
        return string.Equals(code?.Trim(), HrDepartmentCode, StringComparison.OrdinalIgnoreCase);
    }

    private async Task PopulateDepartmentHeadListAsync(
        Department department,
        int? selectedEmployeeId = null)
    {
        var isHrDepartment = IsHRDepartment(department.Code);
        var requiredRole = isHrDepartment ? "HR Head" : "Head Manager";

        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Role == requiredRole &&
                        e.EmploymentStatus == "Active" &&
                        e.DepartmentId == department.DepartmentId)
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .ToListAsync();

        ViewBag.DepartmentHeads = employees.Select(e => new SelectListItem
        {
            Value = e.EmployeeId.ToString(),
            Text = $"{e.EmployeeCode} - {e.FirstName} {e.LastName}",
            Selected = selectedEmployeeId == e.EmployeeId
        }).ToList();
    }

}
