using EMPTRACK.Data;
using EMPTRACK.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<EmployeeCodeService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<LeaveBalanceService>();
builder.Services.AddHostedService<LeaveBalanceAccrualWorker>();

builder.Services.AddDataProtection()
    .SetApplicationName("EMPTRACK")
    .PersistKeysToFileSystem(
        new DirectoryInfo(
            Path.Combine(
                builder.Environment.ContentRootPath,
                "DataProtection-Keys")));

builder.Services.AddSingleton<IPasswordProtectionService, PasswordProtectionService>();

// Cookie authentication.
builder.Services.AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        // Enforce the agreed idle lifetime regardless of Remember Me.
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true; // Renew only with activity: 30-minute idle timeout. Remember Me is not used to extend it.
        options.Events.OnValidatePrincipal = async context =>
        {
            var idClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var userAccountId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            var account = await db.UserAccounts
                .AsNoTracking()
                .Include(a => a.Employee)
                .SingleOrDefaultAsync(a => a.UserAccountId == userAccountId);

            var isAdminAccount = string.Equals(account?.Username, "admin", StringComparison.OrdinalIgnoreCase);
            var employeeIsActive = account?.Employee != null &&
                string.Equals(account.Employee.EmploymentStatus, "Active", StringComparison.OrdinalIgnoreCase);
            if (account == null || !account.IsActive || (!isAdminAccount && !employeeIsActive))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            // Refresh role and employee claims from the database so role changes do not
            // leave stale permissions in an otherwise valid session.
            var role = isAdminAccount ? "Admin" : account.Employee!.Role;
            var refreshedClaims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, account.UserAccountId.ToString()),
                new(ClaimTypes.Name, account.Username),
                new(ClaimTypes.Role, role)
            };
            if (!isAdminAccount && account.EmployeeId.HasValue)
                refreshedClaims.Add(new Claim("EmployeeId", account.EmployeeId.Value.ToString()));
            var refreshedIdentity = new ClaimsIdentity(refreshedClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            context.ReplacePrincipal(new ClaimsPrincipal(refreshedIdentity));
        };
    });

builder.Services.AddControllersWithViews(options =>
{
    // Validate antiforgery tokens for all unsafe MVC requests by default.
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();

// Centralized error handling in non-development environments.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/Index");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Authentication must come before authorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Root URL opens Login. Authenticated users are redirected to Dashboard
// by AccountController.Login().
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();
