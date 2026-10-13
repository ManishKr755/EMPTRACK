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
public class TeamsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public TeamsController(ApplicationDbContext context, IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
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

        return View(team);
    }

    // GET: /Teams/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateDepartmentListAsync();
        await PopulateManagerListAsync(null);

        return View(new TeamCreateViewModel());
    }

    // POST: /Teams/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        TeamCreateViewModel model)
    {
        model.Name = model.Name?.Trim() ?? string.Empty;

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
            await _auditLogService.LogAsync(GetCurrentUserAccountIdForAudit(), "CREATE", "Team", $"Team {team.TeamId} - {team.Name} was created.");
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
            await _auditLogService.LogAsync(
            GetCurrentUserAccountIdForAudit(),
                "UPDATE",
                "Team",
                $"Team {existingTeam.TeamId} - {existingTeam.Name} was updated.");
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
        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();

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
    private int GetCurrentUserAccountIdForAudit()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (claim == null || !int.TryParse(claim.Value, out var userAccountId))
        {
            throw new InvalidOperationException("The authenticated UserAccountId could not be determined.");
        }

        return userAccountId;
    }

}