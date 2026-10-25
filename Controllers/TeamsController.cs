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

[Authorize(Roles = "Admin,HR Head,Head Manager,Manager,HR,Employee")]
public class TeamsController : Controller
{
    private readonly ApplicationDbContext _context;

    public TeamsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Teams
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? departmentId)
    {
        var query = _context.Teams
            .Include(t => t.Department)
            .Include(t => t.ManagerEmployee)
            .Include(t => t.Employees)
            .AsNoTracking()
            .AsQueryable();

        var current = await GetCurrentEmployeeAsync();
        if (!User.IsInRole("Admin"))
        {
            if (current == null) return Forbid();
            if (current.Role == "Manager" && current.TeamId.HasValue)
                query = query.Where(t => t.TeamId == current.TeamId.Value);
            else if (current.Role == "Head Manager" && current.DepartmentId.HasValue)
                query = query.Where(t => t.DepartmentId == current.DepartmentId.Value);
            else if (current.Role == "HR Head")
            {
                var departmentIds = await _context.Departments.AsNoTracking().Where(d => d.DepartmentId == current.DepartmentId || d.HeadHREmployeeId == current.EmployeeId).Select(d => d.DepartmentId).ToListAsync();
                query = query.Where(t => departmentIds.Contains(t.DepartmentId));
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(t =>
                t.Name.Contains(search) ||
                t.Department.Name.Contains(search) ||
                t.Department.Code.Contains(search) ||
                t.ManagerEmployee.FirstName.Contains(search) ||
                t.ManagerEmployee.LastName.Contains(search) ||
                t.ManagerEmployee.EmployeeCode.Contains(search));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(t =>
                t.DepartmentId == departmentId.Value);
        }

        ViewData["Search"] = search;
        ViewData["DepartmentId"] = departmentId;

        await PopulateDepartmentFilterAsync(departmentId);

        var teams = await query
            .OrderBy(t => t.Department.Name)
            .ThenBy(t => t.Name)
            .ToListAsync();

        return View(teams);
    }

    // GET: /Teams/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var team = await _context.Teams
            .Include(t => t.Department)
            .Include(t => t.ManagerEmployee)
            .Include(t => t.Employees)
            .AsNoTracking()
            .SingleOrDefaultAsync(t =>
                t.TeamId == id.Value);

        if (team == null)
        {
            return NotFound();
        }

        if (!await CanReadTeamAsync(team)) return NotFound();
        return View(team);
    }

    // GET: /Teams/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (User.IsInRole("Admin") ||
            !(User.IsInRole("HR Head") || User.IsInRole("Head Manager")))
            return Forbid();
        var current = await GetCurrentEmployeeAsync();
        await PopulateDepartmentListAsync(current?.DepartmentId);
        await PopulateManagerListAsync(current?.DepartmentId);

        return View(new TeamCreateViewModel());
    }

    // POST: /Teams/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        TeamCreateViewModel model)
    {
        if (User.IsInRole("Admin") ||
            !(User.IsInRole("HR Head") || User.IsInRole("Head Manager")))
            return Forbid();
        model.Name = model.Name?.Trim() ?? string.Empty;
        if (!await CanManageDepartmentAsync(model.DepartmentId)) return Forbid();

        await ValidateTeamAsync(
            model.DepartmentId,
            model.Name,
            model.ManagerEmployeeId);

        if (!ModelState.IsValid)
        {
            await PopulateDepartmentListAsync(model.DepartmentId);
            await PopulateManagerListAsync(
                model.DepartmentId,
                model.ManagerEmployeeId);

            return View(model);
        }

        var team = new Team
        {
            DepartmentId = model.DepartmentId,
            Name = model.Name,
            ManagerEmployeeId = model.ManagerEmployeeId
        };

        _context.Teams.Add(team);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The team could not be saved. The team name or manager may already be in use.");

            await PopulateDepartmentListAsync(model.DepartmentId);
            await PopulateManagerListAsync(
                model.DepartmentId,
                model.ManagerEmployeeId);

            return View(model);
        }

        TempData["SuccessMessage"] =
            "Team created successfully.";

        return RedirectToAction(nameof(Index));
    }

    // GET: /Teams/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!(User.IsInRole("Admin") || User.IsInRole("HR Head") || User.IsInRole("Head Manager")))
            return Forbid();
        if (!id.HasValue)
        {
            return NotFound();
        }

        var team = await _context.Teams
            .AsNoTracking()
            .SingleOrDefaultAsync(t =>
                t.TeamId == id.Value);

        if (team == null)
        {
            return NotFound();
        }
        if (!await CanManageTeamAsync(team)) return Forbid();

        var model = new TeamEditViewModel
        {
            TeamId = team.TeamId,
            DepartmentId = team.DepartmentId,
            Name = team.Name,
            ManagerEmployeeId = team.ManagerEmployeeId
        };

        await PopulateDepartmentListAsync(
            model.DepartmentId);

        await PopulateManagerListAsync(
            model.DepartmentId,
            model.ManagerEmployeeId);

        return View(model);
    }

    // POST: /Teams/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        TeamEditViewModel model)
    {
        if (!(User.IsInRole("Admin") || User.IsInRole("HR Head") || User.IsInRole("Head Manager")))
            return Forbid();
        if (id != model.TeamId)
        {
            return BadRequest();
        }

        model.Name = model.Name?.Trim() ?? string.Empty;

        var existingTeam = await _context.Teams
            .SingleOrDefaultAsync(t =>
                t.TeamId == id);

        if (existingTeam == null)
        {
            return NotFound();
        }
        if (!await CanManageTeamAsync(existingTeam) || (!User.IsInRole("Admin") && !await CanManageDepartmentAsync(model.DepartmentId))) return Forbid();

        // A Team Department cannot be changed while employees are assigned
        // to the Team because Employee.DepartmentId must stay consistent
        // with Employee.TeamId -> Team.DepartmentId.
        if (model.DepartmentId != existingTeam.DepartmentId)
        {
            var hasAssignedEmployees = await _context.Employees
                .AnyAsync(e => e.TeamId == existingTeam.TeamId);

            if (hasAssignedEmployees)
            {
                ModelState.AddModelError(
                    nameof(model.DepartmentId),
                    "This team's department cannot be changed while employees are assigned to it.");
            }
        }

        await ValidateTeamAsync(
            model.DepartmentId,
            model.Name,
            model.ManagerEmployeeId,
            id);

        if (!ModelState.IsValid)
        {
            await PopulateDepartmentListAsync(
                model.DepartmentId);

            await PopulateManagerListAsync(
                model.DepartmentId,
                model.ManagerEmployeeId);

            return View(model);
        }

        existingTeam.DepartmentId =
            model.DepartmentId;

        existingTeam.Name =
            model.Name;

        existingTeam.ManagerEmployeeId =
            model.ManagerEmployeeId;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The team could not be updated. The team name or manager may already be in use.");

            await PopulateDepartmentListAsync(
                model.DepartmentId);

            await PopulateManagerListAsync(
                model.DepartmentId,
                model.ManagerEmployeeId);

            return View(model);
        }

        TempData["SuccessMessage"] =
            "Team updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (!id.HasValue) return NotFound();
        if (!(User.IsInRole("Admin") || User.IsInRole("HR Head") || User.IsInRole("Head Manager"))) return Forbid();
        var team = await _context.Teams.Include(t => t.Department).Include(t => t.ManagerEmployee).AsNoTracking().SingleOrDefaultAsync(t => t.TeamId == id.Value);
        if (team == null) return NotFound();
        if (!await CanManageTeamAsync(team)) return Forbid();
        if (await _context.Employees.AnyAsync(e => e.TeamId == team.TeamId))
        {
            TempData["ErrorMessage"] = "This team cannot be deleted while employees are assigned to it. Reassign employees first.";
            return RedirectToAction(nameof(Details), new { id = team.TeamId });
        }
        return View(team);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!(User.IsInRole("Admin") || User.IsInRole("HR Head") || User.IsInRole("Head Manager"))) return Forbid();
        var team = await _context.Teams.SingleOrDefaultAsync(t => t.TeamId == id);
        if (team == null) return NotFound();
        if (!await CanManageTeamAsync(team)) return Forbid();
        if (await _context.Employees.AnyAsync(e => e.TeamId == team.TeamId))
        {
            TempData["ErrorMessage"] = "This team cannot be deleted while employees are assigned to it.";
            return RedirectToAction(nameof(Details), new { id });
        }
        _context.Teams.Remove(team);
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateException) { TempData["ErrorMessage"] = "This team is referenced by existing records and cannot be deleted."; return RedirectToAction(nameof(Index)); }
        TempData["SuccessMessage"] = "Team deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<Employee?> GetCurrentEmployeeAsync()
    {
        var raw = User.FindFirst("EmployeeId")?.Value;
        if (!int.TryParse(raw, out var id)) return null;
        return await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == id);
    }

    private async Task<bool> CanManageDepartmentAsync(int departmentId)
    {
        if (User.IsInRole("Admin")) return false; // Admin is explicitly not permitted to create teams.
        var employee = await GetCurrentEmployeeAsync();
        if (employee == null) return false;
        if (employee.Role == "Head Manager") return employee.DepartmentId == departmentId;
        if (employee.Role == "HR Head")
            return employee.DepartmentId == departmentId || await _context.Departments.AnyAsync(d => d.DepartmentId == departmentId && d.HeadHREmployeeId == employee.EmployeeId);
        return false;
    }

    private async Task<bool> CanManageTeamAsync(Team team)
    {
        if (User.IsInRole("Admin")) return true;
        return await CanManageDepartmentAsync(team.DepartmentId);
    }

    private async Task<bool> CanReadTeamAsync(Team team)
    {
        if (User.IsInRole("Admin")) return true;
        var employee = await GetCurrentEmployeeAsync();
        if (employee == null) return false;
        if (employee.Role == "Manager") return employee.TeamId == team.TeamId;
        if (employee.Role == "Head Manager") return employee.DepartmentId == team.DepartmentId;
        if (employee.Role == "HR Head") return employee.DepartmentId == team.DepartmentId || await _context.Departments.AnyAsync(d => d.DepartmentId == team.DepartmentId && d.HeadHREmployeeId == employee.EmployeeId);
        return true;
    }

    private async Task ValidateTeamAsync(
        int departmentId,
        string teamName,
        int managerEmployeeId,
        int? existingTeamId = null)
    {
        var departmentExists = await _context.Departments
            .AnyAsync(d =>
                d.DepartmentId == departmentId);

        if (!departmentExists)
        {
            ModelState.AddModelError(
                nameof(TeamCreateViewModel.DepartmentId),
                "The selected department does not exist.");

            return;
        }

        if (string.IsNullOrWhiteSpace(teamName))
        {
            ModelState.AddModelError(
                nameof(TeamCreateViewModel.Name),
                "Team name is required.");
        }

        var duplicateTeamName = await _context.Teams
            .AnyAsync(t =>
                t.DepartmentId == departmentId &&
                t.Name.ToUpper() == teamName.ToUpper() &&
                (!existingTeamId.HasValue ||
                 t.TeamId != existingTeamId.Value));

        if (duplicateTeamName)
        {
            ModelState.AddModelError(
                nameof(TeamCreateViewModel.Name),
                "A team with this name already exists in the selected department.");
        }

        var manager = await _context.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(e =>
                e.EmployeeId == managerEmployeeId);

        if (manager == null)
        {
            ModelState.AddModelError(
                nameof(TeamCreateViewModel.ManagerEmployeeId),
                "The selected manager does not exist.");

            return;
        }

        if (!string.Equals(
                manager.Role,
                "Manager",
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(TeamCreateViewModel.ManagerEmployeeId),
                "The selected employee must have the Manager role.");
        }

        if (!string.Equals(
                manager.EmploymentStatus,
                "Active",
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(TeamCreateViewModel.ManagerEmployeeId),
                "The selected manager must be an active employee.");
        }

        if (manager.DepartmentId != departmentId)
        {
            ModelState.AddModelError(
                nameof(TeamCreateViewModel.ManagerEmployeeId),
                "The manager must belong to the selected department.");
        }

        var managerAlreadyAssigned = await _context.Teams
            .AnyAsync(t =>
                t.ManagerEmployeeId == managerEmployeeId &&
                (!existingTeamId.HasValue ||
                 t.TeamId != existingTeamId.Value));

        if (managerAlreadyAssigned)
        {
            ModelState.AddModelError(
                nameof(TeamCreateViewModel.ManagerEmployeeId),
                "This manager is already assigned to another team.");
        }
    }

    private async Task PopulateDepartmentListAsync(
        int? selectedDepartmentId = null)
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();

        ViewBag.Departments = departments
            .Select(d => new SelectListItem
            {
                Value = d.DepartmentId.ToString(),
                Text = $"{d.Code} - {d.Name}",
                Selected =
                    selectedDepartmentId == d.DepartmentId
            })
            .ToList();
    }

    private async Task PopulateManagerListAsync(
        int? departmentId,
        int? selectedManagerId = null)
    {
        var query = _context.Employees
            .AsNoTracking()
            .Where(e =>
                e.Role == "Manager" &&
                e.EmploymentStatus == "Active");

        if (departmentId.HasValue)
        {
            query = query.Where(e =>
                e.DepartmentId == departmentId.Value);
        }

        var managers = await query
            .OrderBy(e => e.FirstName)
            .ThenBy(e => e.LastName)
            .ToListAsync();

        ViewBag.Managers = managers
            .Select(e => new SelectListItem
            {
                Value = e.EmployeeId.ToString(),
                Text =
                    $"{e.EmployeeCode} - {e.FirstName} {e.LastName}",
                Selected =
                    selectedManagerId == e.EmployeeId
            })
            .ToList();
    }

    private async Task PopulateDepartmentFilterAsync(
        int? selectedDepartmentId = null)
    {
        var departmentsQuery = _context.Departments.AsNoTracking().AsQueryable();
        if (!User.IsInRole("Admin") && (User.IsInRole("Manager") || User.IsInRole("Head Manager") || User.IsInRole("HR Head")))
        {
            var current = await GetCurrentEmployeeAsync();
            if (current == null) { ViewBag.DepartmentFilter = new List<SelectListItem>(); return; }
            if (current.Role == "Manager") departmentsQuery = current.DepartmentId.HasValue ? departmentsQuery.Where(d => d.DepartmentId == current.DepartmentId.Value) : departmentsQuery.Where(d => false);
            else if (current.Role == "Head Manager") departmentsQuery = current.DepartmentId.HasValue ? departmentsQuery.Where(d => d.DepartmentId == current.DepartmentId.Value) : departmentsQuery.Where(d => false);
            else
            {
                var departmentIds = await _context.Departments.AsNoTracking().Where(d => d.DepartmentId == current.DepartmentId || d.HeadHREmployeeId == current.EmployeeId).Select(d => d.DepartmentId).ToListAsync();
                departmentsQuery = departmentsQuery.Where(d => departmentIds.Contains(d.DepartmentId));
            }
        }
        var departments = await departmentsQuery.OrderBy(d => d.Name).ToListAsync();

        ViewBag.DepartmentFilter = departments
            .Select(d => new SelectListItem
            {
                Value = d.DepartmentId.ToString(),
                Text = $"{d.Code} - {d.Name}",
                Selected =
                    selectedDepartmentId == d.DepartmentId
            })
            .ToList();
    }
}