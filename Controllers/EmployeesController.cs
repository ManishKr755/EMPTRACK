using EMPTRACK.Data;
using EMPTRACK.Models;
using EMPTRACK.Models.ViewModels;
using EMPTRACK.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace EMPTRACK.Controllers;

[Authorize(Roles = "Admin,HR Head,HR,Head Manager,Manager")]
public class EmployeesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly EmployeeCodeService _employeeCodeService;
    private readonly PasswordHasher<UserAccount> _passwordHasher;
    private readonly IPasswordProtectionService _passwordProtection;

    public EmployeesController(
        ApplicationDbContext context,
        EmployeeCodeService employeeCodeService,
        IAuditLogService auditLogService,
        IPasswordProtectionService passwordProtection)
    {
        _context = context;
        _employeeCodeService = employeeCodeService;
        _auditLogService = auditLogService;
        _passwordHasher = new PasswordHasher<UserAccount>();
        _passwordProtection = passwordProtection;
    }

    // ============================================================
    // INDEX
    // ============================================================

    [HttpGet]
    public async System.Threading.Tasks.Task<IActionResult> Index(
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
            query = query.Where(e =>
                e.Role == role);
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

        var viewModel = new EmployeeIndexViewModel
        {
            Search = search,
            DepartmentId = departmentId,
            Role = role,
            EmploymentStatus = employmentStatus,
            Employees = employees,

            Departments = await _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .ToListAsync(),

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
    public async System.Threading.Tasks.Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var employee = await _context.Employees
            .AsNoTracking()
            .Include(e => e.Department)
            .Include(e => e.Team)
            .Include(e => e.UserAccounts)
            .FirstOrDefaultAsync(e => e.EmployeeId == id.Value);

        if (employee == null) return NotFound();

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
            Username = User.IsInRole("Admin") ? account?.Username : null,
            Password = User.IsInRole("Admin") ? _passwordProtection.Unprotect(account?.EncryptedPassword) : null
        };

        return View(model);
    }

    // ============================================================
    // CREATE - GET
    // ============================================================

    [HttpGet]
    [Authorize(Roles = "Admin,HR Head")]
    public async System.Threading.Tasks.Task<IActionResult> Create()
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
    [Authorize(Roles = "Admin,HR Head")]
    [ValidateAntiForgeryToken]
    public async System.Threading.Tasks.Task<IActionResult> Create(EmployeeCreateViewModel model)
    {
        await ValidateEmployeeRulesAsync(model);

        if (!ModelState.IsValid)
        {
            await LoadCreateFormDataAsync(model);
            return View(model);
        }

        var department = await _context.Departments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DepartmentId == model.DepartmentId);
        if (department == null)
        {
            ModelState.AddModelError(nameof(model.DepartmentId), "The selected department was not found.");
            await LoadCreateFormDataAsync(model);
            return View(model);
        }

        int employeeSerial;
        try { employeeSerial = await _employeeCodeService.GenerateEmployeeSerialAsync(); }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadCreateFormDataAsync(model);
            return View(model);
        }

        string employeeCode;
        try { employeeCode = _employeeCodeService.GenerateEmployeeCode(department.Code, model.Role, employeeSerial); }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadCreateFormDataAsync(model);
            return View(model);
        }

        if (await _employeeCodeService.EmployeeCodeExistsAsync(employeeCode))
        {
            ModelState.AddModelError(string.Empty, "The generated EmployeeCode already exists. Please try again.");
            await LoadCreateFormDataAsync(model);
            return View(model);
        }

        var firstName = model.FirstName.Trim();
        var username = BuildUsername(firstName, employeeCode);
        if (await _context.UserAccounts.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
        {
            ModelState.AddModelError(nameof(model.FirstName), "The generated username is already in use.");
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
            Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
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
        account.PasswordHash = _passwordHasher.HashPassword(account, model.InitialPassword);
        account.EncryptedPassword = _passwordProtection.Protect(model.InitialPassword);
        employee.UserAccounts.Add(account);

        _context.Employees.Add(employee);
        try
        {
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(GetCurrentUserAccountIdForAudit(), "CREATE", "Employee", $"Employee {employee.EmployeeCode} and login account {username} were created.");
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The employee could not be saved. The EmployeeCode, Email, or username may already exist.");
            await LoadCreateFormDataAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Employee {employee.EmployeeCode} was created successfully. Username: {username}.";
        return RedirectToAction(nameof(Details), new { id = employee.EmployeeId });
    }

    // ============================================================
    // EDIT - GET
    // ============================================================

    [HttpGet]
    public async System.Threading.Tasks.Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeId == id.Value);
        if (employee == null) return NotFound();

        var account = await _context.UserAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.EmployeeId == employee.EmployeeId);

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
            Username = User.IsInRole("Admin") ? account?.Username : null,
            CurrentPassword = User.IsInRole("Admin") ? _passwordProtection.Unprotect(account?.EncryptedPassword) : null
        };

        await LoadEditFormDataAsync(model);
        return View(model);
    }

    // ============================================================
    // EDIT - POST
    // ============================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async System.Threading.Tasks.Task<IActionResult> Edit(int id, EmployeeEditViewModel model)
    {
        if (id != model.EmployeeId) return NotFound();

        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == id);
        if (employee == null) return NotFound();

        await ValidateEmployeeEditRulesAsync(model, employee);

        var account = await _context.UserAccounts.SingleOrDefaultAsync(u => u.EmployeeId == employee.EmployeeId);
        var newPassword = model.NewPassword;
        var confirmPassword = model.ConfirmNewPassword;
        var passwordRequested = !string.IsNullOrWhiteSpace(newPassword) || !string.IsNullOrWhiteSpace(confirmPassword);

        if (passwordRequested)
        {
            if (!User.IsInRole("Admin"))
                ModelState.AddModelError(string.Empty, "Only Admin can change another employee's password.");
            if (account == null)
                ModelState.AddModelError(string.Empty, "This employee does not have a login account.");
            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
                ModelState.AddModelError(nameof(model.NewPassword), "Enter both new password fields to change the password.");
            else
            {
                ValidatePasswordRules(newPassword, nameof(model.NewPassword));
                if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
                    ModelState.AddModelError(nameof(model.ConfirmNewPassword), "Passwords do not match.");
            }
        }

        if (!ModelState.IsValid)
        {
            model.EmployeeId = employee.EmployeeId;
            model.EmployeeSerial = employee.EmployeeSerial;
            model.EmployeeCode = employee.EmployeeCode;
            model.Username = User.IsInRole("Admin") ? account?.Username : null;
            model.CurrentPassword = User.IsInRole("Admin") ? _passwordProtection.Unprotect(account?.EncryptedPassword) : null;
            await LoadEditFormDataAsync(model);
            return View(model);
        }

        // EmployeeCode is permanent. It is never regenerated when department/role changes.
        employee.FirstName = model.FirstName.Trim();
        employee.LastName = model.LastName.Trim();
        employee.Email = model.Email.Trim();
        employee.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
        employee.Role = model.Role;
        employee.DepartmentId = model.DepartmentId;
        employee.TeamId = model.TeamId;
        employee.JoiningDate = model.JoiningDate;
        employee.EmploymentStatus = model.EmploymentStatus;

        if (account != null)
        {
            var username = BuildUsername(employee.FirstName, employee.EmployeeCode);
            var duplicate = await _context.UserAccounts.AnyAsync(u => u.UserAccountId != account.UserAccountId && u.Username.ToLower() == username.ToLower());
            if (duplicate)
            {
                ModelState.AddModelError(nameof(model.FirstName), "The generated username is already in use.");
                model.CurrentPassword = User.IsInRole("Admin") ? _passwordProtection.Unprotect(account.EncryptedPassword) : null;
                await LoadEditFormDataAsync(model);
                return View(model);
            }

            account.Username = username;
            account.IsActive = IsEmploymentActive(employee.EmploymentStatus);

            if (passwordRequested)
            {
                account.PasswordHash = _passwordHasher.HashPassword(account, newPassword!);
                account.EncryptedPassword = _passwordProtection.Protect(newPassword!);
            }
        }

        try
        {
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(GetCurrentUserAccountIdForAudit(), "UPDATE", "Employee", $"Employee {employee.EmployeeCode} was updated.");
            if (passwordRequested)
                await _auditLogService.LogAsync(GetCurrentUserAccountIdForAudit(), "PASSWORD_CHANGE", "UserAccount", $"Password for employee {employee.EmployeeCode} was changed by Admin.");
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The employee could not be updated. The Email or username may already exist.");
            model.EmployeeId = employee.EmployeeId;
            model.EmployeeSerial = employee.EmployeeSerial;
            model.EmployeeCode = employee.EmployeeCode;
            model.Username = User.IsInRole("Admin") ? account?.Username : null;
            model.CurrentPassword = User.IsInRole("Admin") ? _passwordProtection.Unprotect(account?.EncryptedPassword) : null;
            await LoadEditFormDataAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Employee {employee.EmployeeCode} was updated successfully.";
        return RedirectToAction(nameof(Details), new { id = employee.EmployeeId });
    }

    // ============================================================
    // CREATE FORM DATA
    // ============================================================

    private async System.Threading.Tasks.Task LoadCreateFormDataAsync(
        EmployeeCreateViewModel model)
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
    }

    // ============================================================
    // ROLES
    // ============================================================

    private static List<string> GetEmployeeRoles(bool isAdmin)
    {
        var roles = new List<string> { "Employee", "Manager", "Head Manager", "HR" };
        if (isAdmin) roles.Add("HR Head");
        return roles;
    }

    // ============================================================
    // EMPLOYMENT STATUSES
    // ============================================================

    private async System.Threading.Tasks.Task<List<string>>
        GetEmploymentStatusesAsync()
    {
        var statuses = await _context.Employees
            .AsNoTracking()
            .Select(e => e.EmploymentStatus)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync();

        // Ensure there are always useful choices.
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
        if (!User.IsInRole("Admin") && !User.IsInRole("HR Head"))
            ModelState.AddModelError(string.Empty, "Only Admin and HR Head can create employees.");

        if (!User.IsInRole("Admin") && model.Role == "HR Head")
            ModelState.AddModelError(nameof(model.Role), "HR Head cannot create another HR Head.");

        if (model.Role == "HR Head" && await _context.Employees.AnyAsync(e => e.Role == "HR Head"))
            ModelState.AddModelError(nameof(model.Role), "Only one HR Head is allowed.");

        ValidatePasswordRules(model.InitialPassword, nameof(model.InitialPassword));

        // Department is required.
        if (!model.DepartmentId.HasValue)
        {
            ModelState.AddModelError(
                nameof(model.DepartmentId),
                "Department is required when creating an employee.");
        }

        // Email uniqueness.
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

        EMPTRACK.Models.Team? team = null;

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

        // Team must belong to selected department.
        if (department != null && team != null)
        {
            if (team.DepartmentId != department.DepartmentId)
            {
                ModelState.AddModelError(
                    nameof(model.TeamId),
                    "Employee Department and Team Department must match.");
            }
        }

        // HR rules.
        if (model.Role == "HR" ||
            model.Role == "HR Head")
        {
            if (department != null &&
                !IsHrDepartment(department))
            {
                ModelState.AddModelError(
                    nameof(model.DepartmentId),
                    "HR employees must belong to the HR department.");
            }

            if (model.TeamId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.TeamId),
                    "HR employees must not be assigned to a Team.");
            }
        }

        // Head Manager cannot belong to a team.
        if (model.Role == "Head Manager")
        {
            if (model.TeamId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.TeamId),
                    "Head Manager must not be assigned to a Team.");
            }
        }

        // Manager must belong to a team.
        if (model.Role == "Manager")
        {
            if (!model.TeamId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.TeamId),
                    "Manager must be assigned to a Team.");
            }
        }
    }

    // ============================================================
    // EDIT VALIDATION
    // ============================================================

    private async System.Threading.Tasks.Task ValidateEmployeeEditRulesAsync(
        EmployeeEditViewModel model,
        EMPTRACK.Models.Employee employee)
    {
        if (model.Role == "HR Head")
        {
            if (!User.IsInRole("Admin"))
                ModelState.AddModelError(nameof(model.Role), "Only Admin can assign HR Head.");

            if (await _context.Employees.AnyAsync(e => e.Role == "HR Head" && e.EmployeeId != employee.EmployeeId))
                ModelState.AddModelError(nameof(model.Role), "Only one HR Head is allowed.");
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

        EMPTRACK.Models.Team? team = null;

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

        // Team must belong to selected department.
        if (department != null && team != null)
        {
            if (team.DepartmentId != department.DepartmentId)
            {
                ModelState.AddModelError(
                    nameof(model.TeamId),
                    "Employee Department and Team Department must match.");
            }
        }

        // HR rules.
        if (model.Role == "HR" ||
            model.Role == "HR Head")
        {
            if (department != null &&
                !IsHrDepartment(department))
            {
                ModelState.AddModelError(
                    nameof(model.DepartmentId),
                    "HR employees must belong to the HR department.");
            }

            if (model.TeamId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.TeamId),
                    "HR employees must not be assigned to a Team.");
            }
        }

        // Head Manager cannot belong to a team.
        if (model.Role == "Head Manager")
        {
            if (model.TeamId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.TeamId),
                    "Head Manager must not be assigned to a Team.");
            }
        }

        // Manager must belong to a team.
        if (model.Role == "Manager")
        {
            if (!model.TeamId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.TeamId),
                    "Manager must be assigned to a Team.");
            }
        }

        // Email must be unique, excluding current employee.
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
    // HR DEPARTMENT CHECK
    // ============================================================

    private static bool IsHrDepartment(
        Department department)
    {
        return department.Name.Equals(
                   "HR",
                   StringComparison.OrdinalIgnoreCase)
               ||
               department.Code.Equals(
                   "01",
                   StringComparison.OrdinalIgnoreCase);
    }
    private static bool IsEmploymentActive(string status) =>
        status.Equals("Active", StringComparison.OrdinalIgnoreCase);

    private static string BuildUsername(string firstName, string employeeCode)
    {
        var cleanFirstName = new string(firstName.Trim().Where(char.IsLetterOrDigit).ToArray());
        return cleanFirstName + employeeCode;
    }

    private void ValidatePasswordRules(string password, string fieldName)
    {
        password ??= string.Empty;
        if (password.Length < 8 || password.Length > 15)
            ModelState.AddModelError(fieldName, "Password must be 8 to 15 characters long.");
        if (password.Any(char.IsWhiteSpace))
            ModelState.AddModelError(fieldName, "Password cannot contain spaces.");
        if (!password.Any(char.IsUpper))
            ModelState.AddModelError(fieldName, "Password must contain at least one uppercase letter.");
        if (!password.Any(char.IsLower))
            ModelState.AddModelError(fieldName, "Password must contain at least one lowercase letter.");
        if (!password.Any(char.IsDigit))
            ModelState.AddModelError(fieldName, "Password must contain at least one number.");
        if (!password.Any(ch => !char.IsLetterOrDigit(ch) && !char.IsWhiteSpace(ch)))
            ModelState.AddModelError(fieldName, "Password must contain at least one special character.");
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
