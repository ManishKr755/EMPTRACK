using EMPTRACK.Data;
using EMPTRACK.Models;
using EMPTRACK.Models.ViewModels;
using EMPTRACK.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace EMPTRACK.Controllers;

[Authorize(Roles = "Admin,HR Head,HR,Head Manager,Manager,Employee")]
public class EmployeesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly EmployeeCodeService _employeeCodeService;
    private readonly PasswordHasher<UserAccount> _passwordHasher;
    private readonly IPasswordProtectionService _passwordProtection;
    private readonly LeaveBalanceService _leaveBalanceService;

    public EmployeesController(
        ApplicationDbContext context,
        EmployeeCodeService employeeCodeService,
        IPasswordProtectionService passwordProtection,
        LeaveBalanceService leaveBalanceService)
    {
        _context = context;
        _employeeCodeService = employeeCodeService;
        _passwordHasher = new PasswordHasher<UserAccount>();
        _passwordProtection = passwordProtection;
        _leaveBalanceService = leaveBalanceService;
    }

    // ============================================================
    // INDEX
    // ============================================================

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        int? departmentId,
        string? role,
        string? employmentStatus)
    {
        var query = _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Team)
            .AsQueryable();

        if (!User.IsInRole("Admin") && !User.IsInRole("HR Head") && !User.IsInRole("HR") && !User.IsInRole("Head Manager") && !User.IsInRole("Manager")) return Forbid();
        if (User.IsInRole("Manager"))
        {
            var currentId = CurrentEmployeeId();
            var teamId = currentId.HasValue ? await _context.Employees.AsNoTracking().Where(e => e.EmployeeId == currentId.Value).Select(e => e.TeamId).FirstOrDefaultAsync() : null;
            query = teamId.HasValue ? query.Where(e => e.TeamId == teamId.Value) : query.Where(e => false);
        }
        else if (User.IsInRole("Head Manager"))
        {
            var currentId = CurrentEmployeeId();
            var deptId = currentId.HasValue ? await _context.Employees.AsNoTracking().Where(e => e.EmployeeId == currentId.Value).Select(e => e.DepartmentId).FirstOrDefaultAsync() : null;
            query = deptId.HasValue ? query.Where(e => e.DepartmentId == deptId.Value) : query.Where(e => false);
        }
        else if (User.IsInRole("HR Head"))
        {
            var currentId = CurrentEmployeeId();
            if (!currentId.HasValue) return Forbid();
            var current = await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentId.Value);
            if (current == null) return Forbid();
            var deptIds = await _context.Departments.AsNoTracking().Where(d => d.DepartmentId == current.DepartmentId || d.HeadHREmployeeId == current.EmployeeId).Select(d => d.DepartmentId).ToListAsync();
            query = query.Where(e => e.DepartmentId.HasValue && deptIds.Contains(e.DepartmentId.Value));
        }
        else if (User.IsInRole("HR"))
        {
            var currentId = CurrentEmployeeId();
            query = currentId.HasValue ? query.Where(e => e.EmployeeId == currentId.Value) : query.Where(e => false);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(e =>
                e.EmployeeCode.Contains(search) ||
                e.FirstName.Contains(search) ||
                e.LastName.Contains(search) ||
                e.Email.Contains(search));
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e =>
                e.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(e => e.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(employmentStatus))
        {
            query = query.Where(e =>
                e.EmploymentStatus == employmentStatus);
        }

        var employees = await query
            .OrderBy(e => e.EmployeeId)
            .Select(e => new EmployeeListItemViewModel
            {
                EmployeeId = e.EmployeeId,
                EmployeeCode = e.EmployeeCode,
                FullName = e.FirstName + " " + e.LastName,
                Email = e.Email,
                Role = e.Role,
                DepartmentName = e.Department != null
                    ? e.Department.Name
                    : null,
                TeamName = e.Team != null
                    ? e.Team.Name
                    : null,
                JoiningDate = e.JoiningDate,
                EmploymentStatus = e.EmploymentStatus
            })
            .ToListAsync();

        var departmentOptions = _context.Departments.AsNoTracking().AsQueryable();
        if (!User.IsInRole("Admin"))
        {
            var currentId = CurrentEmployeeId();
            var current = currentId.HasValue ? await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentId.Value) : null;
            if (current == null) return Forbid();
            if (current.Role == "HR Head")
            {
                var departmentIds = await _context.Departments.AsNoTracking().Where(d => d.DepartmentId == current.DepartmentId || d.HeadHREmployeeId == current.EmployeeId).Select(d => d.DepartmentId).ToListAsync();
                departmentOptions = departmentOptions.Where(d => departmentIds.Contains(d.DepartmentId));
            }
            else if (current.DepartmentId.HasValue) departmentOptions = departmentOptions.Where(d => d.DepartmentId == current.DepartmentId.Value);
            else departmentOptions = departmentOptions.Where(d => false);
        }

        var viewModel = new EmployeeIndexViewModel
        {
            Search = search,
            DepartmentId = departmentId,
            Role = role,
            EmploymentStatus = employmentStatus,
            Employees = employees,

            Departments = await departmentOptions.OrderBy(d => d.Name).ToListAsync(),

            Roles = await _context.Employees
                .AsNoTracking()
                .Select(e => e.Role)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync(),

            EmploymentStatuses = await _context.Employees
                .AsNoTracking()
                .Select(e => e.EmploymentStatus)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync()
        };

        return View(viewModel);
    }

    // ============================================================
    // DETAILS
    // ============================================================

    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Team)
            .Include(e => e.UserAccounts)
            .FirstOrDefaultAsync(e => e.EmployeeId == id.Value);

        if (employee == null)
            return NotFound();
        if (!await CanViewEmployeeAsync(employee)) return NotFound();

        var account = employee.UserAccounts.SingleOrDefault();

        var model = new EmployeeDetailsViewModel
        {
            EmployeeId = employee.EmployeeId,
            EmployeeCode = employee.EmployeeCode,
            EmployeeSerial = employee.EmployeeSerial,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Phone = employee.Phone,
            Role = employee.Role,
            DepartmentName = employee.Department?.Name,
            TeamName = employee.Team?.Name,
            JoiningDate = employee.JoiningDate,
            EmploymentStatus = employee.EmploymentStatus,
            Username = User.IsInRole("Admin")
                ? account?.Username
                : null,
            Password = null
        };

        return View(model);
    }

    // ============================================================
    // CREATE - GET
    // ============================================================

    [HttpGet]
    [Authorize(Roles = "Admin,HR Head,Head Manager")]
    public async Task<IActionResult> Create()
    {
        var model = new EmployeeCreateViewModel
        {
            JoiningDate = DateOnly.FromDateTime(DateTime.Today),
            EmploymentStatus = "Active"
        };

        await LoadCreateFormDataAsync(model);

        return View(model);
    }

    // ============================================================
    // CREATE - POST
    // ============================================================

    [HttpPost]
    [Authorize(Roles = "Admin,HR Head,Head Manager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeCreateViewModel model)
    {
        await ValidateEmployeeRulesAsync(model);

        if (!ModelState.IsValid)
        {
            await LoadCreateFormDataAsync(model);
            return View(model);
        }

        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d =>
                d.DepartmentId == model.DepartmentId);

        if (department == null)
        {
            ModelState.AddModelError(
                nameof(model.DepartmentId),
                "The selected department was not found.");

            await LoadCreateFormDataAsync(model);
            return View(model);
        }

        int employeeSerial;

        try
        {
            employeeSerial =
                await _employeeCodeService.GenerateEmployeeSerialAsync();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            await LoadCreateFormDataAsync(model);
            return View(model);
        }

        string employeeCode;

        try
        {
            employeeCode = _employeeCodeService.GenerateEmployeeCode(
                department.Code,
                model.Role,
                employeeSerial);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            await LoadCreateFormDataAsync(model);
            return View(model);
        }

        if (await _employeeCodeService.EmployeeCodeExistsAsync(employeeCode))
        {
            ModelState.AddModelError(
                string.Empty,
                "The generated EmployeeCode already exists. Please try again.");

            await LoadCreateFormDataAsync(model);
            return View(model);
        }

        var firstName = model.FirstName.Trim();
        var username = BuildUsername(firstName, employeeCode);

        if (await _context.UserAccounts.AnyAsync(u =>
                u.Username.ToLower() == username.ToLower()))
        {
            ModelState.AddModelError(
                nameof(model.FirstName),
                "The generated username is already in use.");

            await LoadCreateFormDataAsync(model);
            return View(model);
        }

        var employee = new Employee
        {
            EmployeeCode = employeeCode,
            EmployeeSerial = employeeSerial,
            FirstName = firstName,
            LastName = model.LastName.Trim(),
            Email = model.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(model.Phone)
                ? null
                : model.Phone.Trim(),
            Role = model.Role,
            DepartmentId = model.DepartmentId,
            TeamId = model.TeamId,
            JoiningDate = model.JoiningDate,
            EmploymentStatus = model.EmploymentStatus
        };

        var account = new UserAccount
        {
            Employee = employee,
            Username = username,
            IsActive = IsEmploymentActive(employee.EmploymentStatus)
        };

        account.PasswordHash =
            _passwordHasher.HashPassword(account, model.InitialPassword);

        account.EncryptedPassword =
            _passwordProtection.Protect(model.InitialPassword);

        employee.UserAccounts.Add(account);
        _context.Employees.Add(employee);

        await using var onboardingTransaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            await _context.SaveChangesAsync();
            await _leaveBalanceService.EnsureAndAccrueAsync();
            await onboardingTransaction.CommitAsync();
        }
        catch (Exception ex) when (ex is DbUpdateException or InvalidOperationException)
        {
            await onboardingTransaction.RollbackAsync();
            ModelState.AddModelError(
                string.Empty,
                "The employee could not be saved. The EmployeeCode, Email, or username may already exist.");

            await LoadCreateFormDataAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] =
            $"Employee {employee.EmployeeCode} was created successfully. Username: {username}.";

        return RedirectToAction(
            nameof(Details),
            new { id = employee.EmployeeId });
    }

    // ============================================================
    // EDIT - GET
    // ============================================================

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeId == id.Value);

        if (employee == null)
            return NotFound();
        if (!User.IsInRole("Admin"))
        {
            var currentId = CurrentEmployeeId();
            if (!(User.IsInRole("HR") || User.IsInRole("Employee")) || currentId != employee.EmployeeId) return Forbid();
        }

        var account = await _context.UserAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(u =>
                u.EmployeeId == employee.EmployeeId);

        var model = new EmployeeEditViewModel
        {
            EmployeeId = employee.EmployeeId,
            EmployeeSerial = employee.EmployeeSerial,
            EmployeeCode = employee.EmployeeCode,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Phone = employee.Phone,
            Role = employee.Role,
            DepartmentId = employee.DepartmentId,
            TeamId = employee.TeamId,
            JoiningDate = employee.JoiningDate,
            EmploymentStatus = employee.EmploymentStatus,
            Username = User.IsInRole("Admin")
                ? account?.Username
                : null,
            CurrentPassword = null
        };

        await LoadEditFormDataAsync(model);

        return View(model);
    }

    // ============================================================
    // EDIT - POST
    // ============================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        EmployeeEditViewModel model)
    {
        if (id != model.EmployeeId)
            return NotFound();

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.EmployeeId == id);

        if (employee == null)
            return NotFound();

        if (!User.IsInRole("Admin"))
        {
            var currentId = CurrentEmployeeId();
            if (!(User.IsInRole("HR") || User.IsInRole("Employee")) || currentId != employee.EmployeeId) return Forbid();
            // Ignore model-state entries for protected fields omitted by the self-service form.
            foreach (var field in new[] { nameof(model.FirstName), nameof(model.LastName), nameof(model.Role), nameof(model.DepartmentId), nameof(model.TeamId), nameof(model.JoiningDate), nameof(model.EmploymentStatus), nameof(model.EmployeeCode), nameof(model.EmployeeSerial), nameof(model.Username), nameof(model.NewPassword), nameof(model.ConfirmNewPassword), nameof(model.CurrentPassword) }) ModelState.Remove(field);
            if (!ModelState.IsValid) return View(model);
            // Self-service is deliberately limited to contact fields. Ignore every protected posted field.
            employee.Email = model.Email.Trim();
            employee.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
            try { await _context.SaveChangesAsync(); }
            catch (DbUpdateException) { ModelState.AddModelError(nameof(model.Email), "The email address is already in use."); return View(model); }
            TempData["SuccessMessage"] = "Your contact details were updated.";
            return RedirectToAction(nameof(Details), new { id = employee.EmployeeId });
        }

        await ValidateEmployeeEditRulesAsync(model, employee);

        var account = await _context.UserAccounts
            .SingleOrDefaultAsync(u =>
                u.EmployeeId == employee.EmployeeId);

        var newPassword = model.NewPassword;
        var confirmPassword = model.ConfirmNewPassword;

        var passwordRequested =
            !string.IsNullOrWhiteSpace(newPassword) ||
            !string.IsNullOrWhiteSpace(confirmPassword);

        if (passwordRequested)
        {
            if (!User.IsInRole("Admin"))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Only Admin can change another employee's password.");
            }

            if (account == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This employee does not have a login account.");
            }

            if (string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError(
                    nameof(model.NewPassword),
                    "Enter both new password fields to change the password.");
            }
            else
            {
                ValidatePasswordRules(
                    newPassword,
                    nameof(model.NewPassword));

                if (!string.Equals(
                        newPassword,
                        confirmPassword,
                        StringComparison.Ordinal))
                {
                    ModelState.AddModelError(
                        nameof(model.ConfirmNewPassword),
                        "Passwords do not match.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            model.EmployeeId = employee.EmployeeId;
            model.EmployeeSerial = employee.EmployeeSerial;
            model.EmployeeCode = employee.EmployeeCode;
            model.Username = User.IsInRole("Admin")
                ? account?.Username
                : null;
            model.CurrentPassword = null;

            await LoadEditFormDataAsync(model);

            return View(model);
        }

        // EmployeeCode remains permanent when department or role changes.
        employee.FirstName = model.FirstName.Trim();
        employee.LastName = model.LastName.Trim();
        employee.Email = model.Email.Trim();
        employee.Phone = string.IsNullOrWhiteSpace(model.Phone)
            ? null
            : model.Phone.Trim();
        employee.Role = model.Role;
        employee.DepartmentId = model.DepartmentId;
        employee.TeamId = model.TeamId;
        employee.JoiningDate = model.JoiningDate;
        employee.EmploymentStatus = model.EmploymentStatus;

        if (account != null)
        {
            var username = BuildUsername(
                employee.FirstName,
                employee.EmployeeCode);

            var duplicate = await _context.UserAccounts.AnyAsync(u =>
                u.UserAccountId != account.UserAccountId &&
                u.Username.ToLower() == username.ToLower());

            if (duplicate)
            {
                ModelState.AddModelError(
                    nameof(model.FirstName),
                    "The generated username is already in use.");

                model.CurrentPassword = null;

                await LoadEditFormDataAsync(model);
                return View(model);
            }

            account.Username = username;
            account.IsActive =
                IsEmploymentActive(employee.EmploymentStatus);

            if (passwordRequested)
            {
                account.PasswordHash =
                    _passwordHasher.HashPassword(account, newPassword!);

                account.EncryptedPassword =
                    _passwordProtection.Protect(newPassword!);
            }
        }

        try
        {
            await _context.SaveChangesAsync();
            await _leaveBalanceService.EnsureAndAccrueAsync();

        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The employee could not be updated. The Email or username may already exist.");

            model.EmployeeId = employee.EmployeeId;
            model.EmployeeSerial = employee.EmployeeSerial;
            model.EmployeeCode = employee.EmployeeCode;
            model.Username = User.IsInRole("Admin")
                ? account?.Username
                : null;
            model.CurrentPassword = null;

            await LoadEditFormDataAsync(model);

            return View(model);
        }

        TempData["SuccessMessage"] =
            $"Employee {employee.EmployeeCode} was updated successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id = employee.EmployeeId });
    }

    // ============================================================
    // CREATE FORM DATA
    // ============================================================

    private int? CurrentEmployeeId()
    {
        return int.TryParse(User.FindFirst("EmployeeId")?.Value, out var id) && id > 0 ? id : null;
    }

    private async Task<bool> CanViewEmployeeAsync(Employee employee)
    {
        if (User.IsInRole("Admin")) return true;
        var currentId = CurrentEmployeeId();
        if (!currentId.HasValue) return false;
        if (User.IsInRole("HR") || User.IsInRole("Employee")) return currentId.Value == employee.EmployeeId;
        var current = await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentId.Value);
        if (current == null) return false;
        if (current.Role == "Manager") return current.TeamId.HasValue && employee.TeamId == current.TeamId;
        if (current.Role == "Head Manager") return current.DepartmentId.HasValue && employee.DepartmentId == current.DepartmentId;
        if (current.Role == "HR Head")
        {
            if (current.DepartmentId == employee.DepartmentId) return true;
            return employee.DepartmentId.HasValue && await _context.Departments.AsNoTracking().AnyAsync(d => d.DepartmentId == employee.DepartmentId.Value && d.HeadHREmployeeId == current.EmployeeId);
        }
        return false;
    }

    private async System.Threading.Tasks.Task LoadCreateFormDataAsync(
        EmployeeCreateViewModel model)
    {
        var departmentsQuery = _context.Departments.AsNoTracking().AsQueryable();
        var teamsQuery = _context.Teams.AsNoTracking().AsQueryable();
        if (!User.IsInRole("Admin"))
        {
            var currentId = CurrentEmployeeId();
            var current = currentId.HasValue ? await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentId.Value) : null;
            if (current == null) return;
            if (User.IsInRole("HR Head"))
            {
                var departmentIds = await _context.Departments.AsNoTracking().Where(d => d.DepartmentId == current.DepartmentId || d.HeadHREmployeeId == current.EmployeeId).Select(d => d.DepartmentId).ToListAsync();
                departmentsQuery = departmentsQuery.Where(d => departmentIds.Contains(d.DepartmentId));
                teamsQuery = teamsQuery.Where(t => departmentIds.Contains(t.DepartmentId));
            }
            else
            {
                departmentsQuery = current.DepartmentId.HasValue ? departmentsQuery.Where(d => d.DepartmentId == current.DepartmentId.Value) : departmentsQuery.Where(d => false);
                teamsQuery = current.DepartmentId.HasValue ? teamsQuery.Where(t => t.DepartmentId == current.DepartmentId.Value) : teamsQuery.Where(t => false);
            }
        }
        ViewBag.Departments = await departmentsQuery.OrderBy(d => d.Name).ToListAsync();
        ViewBag.Teams = await teamsQuery.OrderBy(t => t.Name).ToListAsync();
        ViewBag.Roles = User.IsInRole("Admin") ? GetEmployeeRoles(true) : User.IsInRole("Head Manager") ? new List<string> { "Employee" } : new List<string> { "Employee", "HR" };

        ViewBag.EmploymentStatuses =
            await GetEmploymentStatusesAsync();

        ViewBag.IsAdmin = User.IsInRole("Admin");

        ViewBag.HrHeadExists = await _context.Employees
            .AsNoTracking()
            .AnyAsync(e => e.Role == "HR Head");

        ViewBag.HeadManagerDepartmentIds = await _context.Employees
            .AsNoTracking()
            .Where(e =>
                e.Role == "Head Manager" &&
                e.DepartmentId.HasValue)
            .Select(e => e.DepartmentId!.Value)
            .Distinct()
            .ToListAsync();
    }

    // ============================================================
    // EDIT FORM DATA
    // ============================================================

    private async System.Threading.Tasks.Task LoadEditFormDataAsync(
        EmployeeEditViewModel model)
    {
        ViewBag.Departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();

        ViewBag.Teams = await _context.Teams
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync();

        ViewBag.Roles = GetEmployeeRoles(User.IsInRole("Admin"));

        ViewBag.EmploymentStatuses =
            await GetEmploymentStatusesAsync();

        ViewBag.IsAdmin = User.IsInRole("Admin");

        ViewBag.HrHeadExists = await _context.Employees
            .AsNoTracking()
            .AnyAsync(e =>
                e.Role == "HR Head" &&
                e.EmployeeId != model.EmployeeId);

        ViewBag.HeadManagerDepartmentIds = await _context.Employees
            .AsNoTracking()
            .Where(e =>
                e.Role == "Head Manager" &&
                e.DepartmentId.HasValue &&
                e.EmployeeId != model.EmployeeId)
            .Select(e => e.DepartmentId!.Value)
            .Distinct()
            .ToListAsync();
    }

    // ============================================================
    // ROLES
    // ============================================================

    private static List<string> GetEmployeeRoles(bool isAdmin)
    {
        var roles = new List<string>
        {
            "Employee",
            "Manager",
            "Head Manager",
            "HR"
        };

        if (isAdmin)
            roles.Add("HR Head");

        return roles;
    }

    // ============================================================
    // EMPLOYMENT STATUSES
    // ============================================================

    private async Task<List<string>> GetEmploymentStatusesAsync()
    {
        var statuses = await _context.Employees
            .AsNoTracking()
            .Select(e => e.EmploymentStatus)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        if (statuses.Count == 0)
        {
            statuses = new List<string>
            {
                "Active",
                "Inactive",
                "On Leave",
                "Resigned",
                "Terminated"
            };
        }

        return statuses;
    }

    // ============================================================
    // CREATE VALIDATION
    // ============================================================

    private async System.Threading.Tasks.Task ValidateEmployeeRulesAsync(
        EmployeeCreateViewModel model)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("HR Head") && !User.IsInRole("Head Manager"))
        {
            ModelState.AddModelError(string.Empty, "You are not authorized to create employees.");
        }

        if (!User.IsInRole("Admin"))
        {
            var currentId = CurrentEmployeeId();
            var current = currentId.HasValue ? await _context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.EmployeeId == currentId.Value) : null;
            var departmentInScope = current != null && model.DepartmentId.HasValue && current.DepartmentId == model.DepartmentId;
            if (!departmentInScope && current != null && User.IsInRole("HR Head") && model.DepartmentId.HasValue)
                departmentInScope = await _context.Departments.AnyAsync(d => d.DepartmentId == model.DepartmentId.Value && d.HeadHREmployeeId == current.EmployeeId);
            if (current == null || !departmentInScope)
                ModelState.AddModelError(nameof(model.DepartmentId), "You may create employees only within your authorized department scope.");
            if (User.IsInRole("Head Manager") && model.Role != "Employee")
                ModelState.AddModelError(nameof(model.Role), "Head Managers may create Employee accounts only.");
            if (User.IsInRole("HR Head") && model.Role != "Employee" && model.Role != "HR")
                ModelState.AddModelError(nameof(model.Role), "HR Head may create Employee or HR accounts only within the HR department scope.");
        }

        if (string.Equals(
                model.Role,
                "HR Head",
                StringComparison.OrdinalIgnoreCase))
        {
            if (!User.IsInRole("Admin"))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "Only Admin can assign HR Head.");
            }

            if (await _context.Employees.AnyAsync(e =>
                    e.Role == "HR Head"))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "An HR Head is already assigned.");
            }
        }

        ValidatePasswordRules(
            model.InitialPassword,
            nameof(model.InitialPassword));

        if (!model.DepartmentId.HasValue)
        {
            ModelState.AddModelError(
                nameof(model.DepartmentId),
                "Department is required when creating an employee.");
        }

        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var email = model.Email.Trim();

            var emailExists = await _context.Employees
                .AnyAsync(e => e.Email == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "An employee with this email already exists.");
            }
        }

        Department? department = null;

        if (model.DepartmentId.HasValue)
        {
            department = await _context.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d =>
                    d.DepartmentId == model.DepartmentId.Value);

            if (department == null)
            {
                ModelState.AddModelError(
                    nameof(model.DepartmentId),
                    "Selected department does not exist.");
            }
        }

        Team? team = null;

        if (model.TeamId.HasValue)
        {
            team = await _context.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.TeamId == model.TeamId.Value);

            if (team == null)
            {
                ModelState.AddModelError(
                    nameof(model.TeamId),
                    "Selected team does not exist.");
            }
        }

        if (department != null && team != null &&
            team.DepartmentId != department.DepartmentId)
        {
            ModelState.AddModelError(
                nameof(model.TeamId),
                "Employee Department and Team Department must match.");
        }

        ValidateRoleDepartmentRules(
            model.Role,
            model.DepartmentId,
            department,
            model.TeamId);

        if (model.Role == "Head Manager" &&
            model.DepartmentId.HasValue)
        {
            var headManagerExists = await _context.Employees
                .AnyAsync(e =>
                    e.Role == "Head Manager" &&
                    e.DepartmentId == model.DepartmentId.Value);

            if (headManagerExists)
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "A Head Manager is already assigned to this department.");
            }
        }
    }

    // ============================================================
    // EDIT VALIDATION
    // ============================================================

    private async System.Threading.Tasks.Task ValidateEmployeeEditRulesAsync(
        EmployeeEditViewModel model,
        Employee employee)
    {
        if (string.Equals(
                model.Role,
                "HR Head",
                StringComparison.OrdinalIgnoreCase))
        {
            if (!User.IsInRole("Admin"))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "Only Admin can assign HR Head.");
            }

            var anotherHrHeadExists = await _context.Employees
                .AnyAsync(e =>
                    e.Role == "HR Head" &&
                    e.EmployeeId != employee.EmployeeId);

            if (anotherHrHeadExists)
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "An HR Head is already assigned.");
            }
        }

        Department? department = null;

        if (model.DepartmentId.HasValue)
        {
            department = await _context.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d =>
                    d.DepartmentId == model.DepartmentId.Value);

            if (department == null)
            {
                ModelState.AddModelError(
                    nameof(model.DepartmentId),
                    "Selected department does not exist.");
            }
        }
        else
        {
            ModelState.AddModelError(
                nameof(model.DepartmentId),
                "Department is required.");
        }

        Team? team = null;

        if (model.TeamId.HasValue)
        {
            team = await _context.Teams
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.TeamId == model.TeamId.Value);

            if (team == null)
            {
                ModelState.AddModelError(
                    nameof(model.TeamId),
                    "Selected team does not exist.");
            }
        }

        if (department != null && team != null &&
            team.DepartmentId != department.DepartmentId)
        {
            ModelState.AddModelError(
                nameof(model.TeamId),
                "Employee Department and Team Department must match.");
        }

        ValidateRoleDepartmentRules(
            model.Role,
            model.DepartmentId,
            department,
            model.TeamId);

        if (model.Role == "Head Manager" &&
            model.DepartmentId.HasValue)
        {
            var anotherHeadManagerExists = await _context.Employees
                .AnyAsync(e =>
                    e.Role == "Head Manager" &&
                    e.DepartmentId == model.DepartmentId.Value &&
                    e.EmployeeId != employee.EmployeeId);

            if (anotherHeadManagerExists)
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "A Head Manager is already assigned to this department.");
            }
        }

        if (!string.IsNullOrWhiteSpace(model.Email))
        {
            var email = model.Email.Trim();

            var emailExists = await _context.Employees
                .AnyAsync(e =>
                    e.Email == email &&
                    e.EmployeeId != employee.EmployeeId);

            if (emailExists)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "Another employee already uses this email.");
            }
        }
    }

    // ============================================================
    // SHARED ROLE / DEPARTMENT VALIDATION
    // ============================================================

    private void ValidateRoleDepartmentRules(
        string? role,
        int? departmentId,
        Department? department,
        int? teamId)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            ModelState.AddModelError(
                nameof(EmployeeCreateViewModel.Role),
                "Role is required.");

            return;
        }

        if (department == null)
            return;

        var isHrRole =
            string.Equals(role, "HR", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "HR Head", StringComparison.OrdinalIgnoreCase);

        var isHrDepartment = IsHrDepartment(department);

        if (isHrRole && !isHrDepartment)
        {
            ModelState.AddModelError(
                nameof(EmployeeCreateViewModel.DepartmentId),
                "HR employees must belong to the HR department.");
        }

        if (!isHrRole && isHrDepartment)
        {
            ModelState.AddModelError(
                nameof(EmployeeCreateViewModel.Role),
                "Select a valid role for the HR department.");
        }

        if (isHrRole && teamId.HasValue)
        {
            ModelState.AddModelError(
                nameof(EmployeeCreateViewModel.TeamId),
                "HR employees must not be assigned to a Team.");
        }

        if (string.Equals(
                role,
                "Head Manager",
                StringComparison.OrdinalIgnoreCase) &&
            teamId.HasValue)
        {
            ModelState.AddModelError(
                nameof(EmployeeCreateViewModel.TeamId),
                "Head Manager must not be assigned to a Team.");
        }

        var allowedRoles = isHrDepartment
            ? new[] { "HR", "HR Head" }
            : new[] { "Employee", "Manager", "Head Manager" };

        if (!allowedRoles.Contains(
                role,
                StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(EmployeeCreateViewModel.Role),
                "The selected role is not valid for this department.");
        }
    }

    // ============================================================
    // HR DEPARTMENT CHECK
    // ============================================================

    private static bool IsHrDepartment(Department department)
    {
        return string.Equals(
                   department.Name,
                   "HR",
                   StringComparison.OrdinalIgnoreCase)
               ||
               string.Equals(
                   department.Code,
                   "01",
                   StringComparison.OrdinalIgnoreCase);
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private static bool IsEmploymentActive(string status) =>
        string.Equals(
            status,
            "Active",
            StringComparison.OrdinalIgnoreCase);

    private static string BuildUsername(
        string firstName,
        string employeeCode)
    {
        var cleanFirstName = new string(
            firstName.Trim()
                .Where(char.IsLetterOrDigit)
                .ToArray());

        return cleanFirstName + employeeCode;
    }

    private void ValidatePasswordRules(
        string? password,
        string fieldName)
    {
        password ??= string.Empty;

        if (password.Length < 8 || password.Length > 15)
        {
            ModelState.AddModelError(
                fieldName,
                "Password must be 8 to 15 characters long.");
        }

        if (password.Any(char.IsWhiteSpace))
        {
            ModelState.AddModelError(
                fieldName,
                "Password cannot contain spaces.");
        }

        if (!password.Any(char.IsUpper))
        {
            ModelState.AddModelError(
                fieldName,
                "Password must contain at least one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            ModelState.AddModelError(
                fieldName,
                "Password must contain at least one lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            ModelState.AddModelError(
                fieldName,
                "Password must contain at least one number.");
        }

        if (!password.Any(ch =>
                !char.IsLetterOrDigit(ch) &&
                !char.IsWhiteSpace(ch)))
        {
            ModelState.AddModelError(
                fieldName,
                "Password must contain at least one special character.");
        }
    }

}