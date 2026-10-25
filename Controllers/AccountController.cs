using EMPTRACK.Data;
using EMPTRACK.Models;
using EMPTRACK.Models.ViewModels;
using EMPTRACK.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Task = System.Threading.Tasks.Task;

namespace EMPTRACK.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<UserAccount> _passwordHasher;
    private readonly IPasswordProtectionService _passwordProtection;

    public AccountController(
        ApplicationDbContext context,
        IPasswordProtectionService passwordProtection)
    {
        _context = context;
        _passwordProtection = passwordProtection;
        _passwordHasher = new PasswordHasher<UserAccount>();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // Authenticated users should not be shown the Login page again.
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var username = model.Username.Trim();
        var userAccount = await _context.UserAccounts
            .Include(u => u.Employee)
            .SingleOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        var isAdminAccount = string.Equals(userAccount?.Username, "admin", StringComparison.OrdinalIgnoreCase);
        var employeeIsActive = userAccount?.Employee != null &&
                               string.Equals(userAccount.Employee.EmploymentStatus, "Active", StringComparison.OrdinalIgnoreCase);

        if (userAccount == null || !userAccount.IsActive || (!isAdminAccount && !employeeIsActive))
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            userAccount, userAccount.PasswordHash, model.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userAccount.UserAccountId.ToString()),
            new(ClaimTypes.Name, userAccount.Username)
        };

        var applicationRole = userAccount.Username.Equals("admin", StringComparison.OrdinalIgnoreCase)
            ? "Admin"
            : userAccount.Employee?.Role ?? "Employee";

        claims.Add(new Claim(ClaimTypes.Role, applicationRole));

        if (!isAdminAccount && userAccount.EmployeeId.HasValue)
            claims.Add(new Claim("EmployeeId", userAccount.EmployeeId.Value.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        ValidatePasswordRules(model.NewPassword, nameof(model.NewPassword));

        if (!ModelState.IsValid)
        {
            TempData["PasswordError"] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Invalid password.";
            return RedirectToAction("Index", "Dashboard");
        }

        var account = await GetCurrentAccountAsync();
        if (account == null)
            return Challenge();

        var result = _passwordHasher.VerifyHashedPassword(
            account, account.PasswordHash, model.CurrentPassword);

        if (result == PasswordVerificationResult.Failed)
        {
            TempData["PasswordError"] = "Current password is incorrect.";
            return RedirectToAction("Index", "Dashboard");
        }

        account.PasswordHash = _passwordHasher.HashPassword(account, model.NewPassword);
        // Keep the Admin's password out of the reversible store; only employee demo accounts need display.
        if (!account.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            account.EncryptedPassword = _passwordProtection.Protect(model.NewPassword);

        await _context.SaveChangesAsync();

        TempData["PasswordSuccess"] = "Password changed successfully.";
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }

    private async Task<UserAccount?> GetCurrentAccountAsync()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out var id))
            return null;
        return await _context.UserAccounts.SingleOrDefaultAsync(u => u.UserAccountId == id);
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
}
