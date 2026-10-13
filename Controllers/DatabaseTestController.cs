using EMPTRACK.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace EMPTRACK.Controllers;

[Authorize(Roles = "Admin")]
public class DatabaseTestController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseTestController> _logger;

    public DatabaseTestController(
        ApplicationDbContext context,
        ILogger<DatabaseTestController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            bool connected = await _context.Database.CanConnectAsync();

            ViewBag.Status = connected ? "Connected" : "Connection failed";
            ViewBag.Database = _context.Database.GetDbConnection().Database;
            ViewBag.Provider = _context.Database.ProviderName;

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EMPTRACK database connection test failed.");

            ViewBag.Status = "Connection failed";
            ViewBag.Database = "Unavailable";
            ViewBag.Provider = "Unavailable";

            return View();
        }
    }
}