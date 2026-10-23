using EMPTRACK.Data;
using EMPTRACK.Models;
using EMPTRACK.Models.ViewModels;
using EMPTRACK.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace EMPTRACK.Controllers;

[Authorize(Roles = "Admin")]
public class UserAccountsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordProtectionService _passwordProtection;
    private readonly PasswordHasher<UserAccount> _passwordHasher;

    public UserAccountsController(ApplicationDbContext context, IPasswordProtectionService passwordProtection)
    {
        _context = context;
        _passwordProtection = passwordProtection;
        _passwordHasher = new PasswordHasher<UserAccount>();
    }

    public async Task<IActionResult> Index(string? search, string? status)
    {
        var query = _context.UserAccounts.Include(u => u.Employee).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(u => u.Username.Contains(search) || (u.Employee != null && (u.Employee.FirstName.Contains(search) || u.Employee.LastName.Contains(search) || u.Employee.EmployeeCode.Contains(search))));
        }
        if (status == "Active") query = query.Where(u => u.IsActive);
        else if (status == "Inactive") query = query.Where(u => !u.IsActive);
        ViewData["Search"] = search; ViewData["Status"] = status;
        return View(await query.OrderBy(u => u.Username).ToListAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue) return NotFound();
        var account = await _context.UserAccounts.Include(u => u.Employee).Include(u => u.ApprovedLeaveRequests).SingleOrDefaultAsync(u => u.UserAccountId == id.Value);
        if (account == null) return NotFound();
        return View(account);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateEmployeesAsync();
        return View(new UserAccountCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserAccountCreateViewModel model)
    {
        ModelState.Remove(nameof(model.Username));
        if (!model.EmployeeId.HasValue) ModelState.AddModelError(nameof(model.EmployeeId), "Employee is required.");
        var employee = model.EmployeeId.HasValue ? await _context.Employees.FindAsync(model.EmployeeId.Value) : null;
        if (model.EmployeeId.HasValue && employee == null) ModelState.AddModelError(nameof(model.EmployeeId), "The selected employee does not exist.");
        if (employee != null && await _context.UserAccounts.AnyAsync(u => u.EmployeeId == employee.EmployeeId)) ModelState.AddModelError(nameof(model.EmployeeId), "This employee already has a user account.");
        if (employee != null)
        {
            model.Username = BuildUsername(employee.FirstName, employee.EmployeeCode);
            ModelState.Remove(nameof(model.Username));
        }
        if (await _context.UserAccounts.AnyAsync(u => u.Username.ToLower() == model.Username.Trim().ToLower())) ModelState.AddModelError(nameof(model.Username), "This username is already in use.");
        ValidatePasswordRules(model.Password, nameof(model.Password));
        if (!ModelState.IsValid) { await PopulateEmployeesAsync(model.EmployeeId); return View(model); }

        var account = new UserAccount { EmployeeId = employee!.EmployeeId, Username = model.Username.Trim(), IsActive = model.IsActive };
        account.PasswordHash = _passwordHasher.HashPassword(account, model.Password);
        account.EncryptedPassword = _passwordProtection.Protect(model.Password);
        _context.UserAccounts.Add(account);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The user account could not be created. The employee or username may already be in use.");
            await PopulateEmployeesAsync(model.EmployeeId); return View(model);
        }
        TempData["SuccessMessage"] = "User account created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue) return NotFound();
        var account = await _context.UserAccounts.Include(u => u.Employee).SingleOrDefaultAsync(u => u.UserAccountId == id.Value);
        if (account == null) return NotFound();
        var model = new UserAccountEditViewModel
        { UserAccountId = account.UserAccountId, EmployeeId = account.EmployeeId, Username = account.Username, IsActive = account.IsActive };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserAccountEditViewModel model)
    {
        if (id != model.UserAccountId) return NotFound();
        var account = await _context.UserAccounts.Include(u => u.Employee).SingleOrDefaultAsync(u => u.UserAccountId == id);
        if (account == null) return NotFound();

        if (account.Employee != null)
        {
            account.Username = BuildUsername(account.Employee.FirstName, account.Employee.EmployeeCode);
            model.Username = account.Username;
            model.EmployeeId = account.EmployeeId;
        }
        else if (account.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
        {
            model.Username = account.Username;
            model.EmployeeId = null;
        }
        if (account.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) && !model.IsActive)
            ModelState.AddModelError(nameof(model.IsActive), "The company Admin account cannot be deactivated here.");

        account.IsActive = model.IsActive;

        if (!ModelState.IsValid) return View(model);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "The user account could not be updated.");
            return View(model);
        }
        TempData["SuccessMessage"] = "User account updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(int? id)
    {
        if (!id.HasValue) return NotFound();
        var account = await _context.UserAccounts.FindAsync(id.Value);
        if (account == null) return NotFound();
        return View(new UserAccountResetPasswordViewModel { UserAccountId = account.UserAccountId, Username = account.Username });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, UserAccountResetPasswordViewModel model)
    {
        if (id != model.UserAccountId) return NotFound();
        var account = await _context.UserAccounts.FindAsync(id);
        if (account == null) return NotFound();
        model.Username = account.Username;
        ValidatePasswordRules(model.NewPassword, nameof(model.NewPassword));
        if (!ModelState.IsValid) return View(model);
        account.PasswordHash = _passwordHasher.HashPassword(account, model.NewPassword);
        account.EncryptedPassword = account.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) ? null : _passwordProtection.Protect(model.NewPassword);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException) { ModelState.AddModelError(string.Empty, "The password could not be updated."); return View(model); }
        TempData["SuccessMessage"] = "Password updated successfully.";
        return RedirectToAction(nameof(Details), new { id = account.UserAccountId });
    }

    private async Task PopulateEmployeesAsync(int? selectedEmployeeId = null)
    {
        var employees = await _context.Employees.Where(e => !_context.UserAccounts.Any(u => u.EmployeeId == e.EmployeeId) || e.EmployeeId == selectedEmployeeId).OrderBy(e => e.FirstName).ThenBy(e => e.LastName).ToListAsync();
        ViewBag.Employees = employees; ViewBag.SelectedEmployeeId = selectedEmployeeId;
    }

    private static string BuildUsername(string firstName, string employeeCode) => new string(firstName.Trim().Where(char.IsLetterOrDigit).ToArray()) + employeeCode;

    private void ValidatePasswordRules(string password, string fieldName)
    {
        password ??= string.Empty;
        if (password.Length < 8 || password.Length > 15) ModelState.AddModelError(fieldName, "Password must be 8 to 15 characters long.");
        if (password.Any(char.IsWhiteSpace)) ModelState.AddModelError(fieldName, "Password cannot contain spaces.");
        if (!password.Any(char.IsUpper)) ModelState.AddModelError(fieldName, "Password must contain at least one uppercase letter.");
        if (!password.Any(char.IsLower)) ModelState.AddModelError(fieldName, "Password must contain at least one lowercase letter.");
        if (!password.Any(char.IsDigit)) ModelState.AddModelError(fieldName, "Password must contain at least one number.");
        if (!password.Any(ch => !char.IsLetterOrDigit(ch) && !char.IsWhiteSpace(ch))) ModelState.AddModelError(fieldName, "Password must contain at least one special character.");
    }

}
