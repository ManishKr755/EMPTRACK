using EMPTRACK.Data;
using EMPTRACK.Models;
using EMPTRACK.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace EMPTRACK.Controllers;

[Authorize(Roles = "Admin,HR Head,HR")]
public class LeaveTypesController : Controller
{
    private readonly ApplicationDbContext _context;

    public LeaveTypesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /LeaveTypes
    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.LeaveTypes
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(l =>
                l.Name.Contains(search));
        }

        ViewData["Search"] = search;

        var leaveTypes = await query
            .OrderBy(l => l.Name)
            .ToListAsync();

        return View(leaveTypes);
    }

    // GET: /LeaveTypes/Details/5
    [HttpGet]
    public async Task<IActionResult> Details(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var leaveType = await _context.LeaveTypes
            .Include(l => l.LeaveBalances)
                .ThenInclude(lb => lb.Employee)
            .Include(l => l.LeaveRequests)
                .ThenInclude(lr => lr.Employee)
            .AsNoTracking()
            .SingleOrDefaultAsync(l =>
                l.LeaveTypeId == id.Value);

        if (leaveType == null)
        {
            return NotFound();
        }

        return View(leaveType);
    }

    // GET: /LeaveTypes/Create
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Create()
    {
        var model = new LeaveTypeCreateViewModel
        {
            IsActive = true
        };

        return View(model);
    }

    // POST: /LeaveTypes/Create
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        LeaveTypeCreateViewModel model)
    {
        var normalizedName = model.Name?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "Leave type name is required.");
        }
        else
        {
            var duplicateExists = await _context.LeaveTypes
                .AnyAsync(l =>
                    l.Name.ToLower() ==
                    normalizedName.ToLower());

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "A leave type with this name already exists.");
            }
        }

        ValidateAllocation(model.AllocationType, model.AllocationLimit);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var leaveType = new LeaveType
        {
            Name = normalizedName!,
            AllocationType = model.AllocationType,
            AllocationLimit = model.AllocationLimit,
            IsActive = model.IsActive
        };

        _context.LeaveTypes.Add(leaveType);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The leave type could not be saved. " +
                "A leave type with this name may already exist.");

            return View(model);
        }

        TempData["SuccessMessage"] =
            "Leave type created successfully.";

        return RedirectToAction(nameof(Index));
    }

    // GET: /LeaveTypes/Edit/5
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var leaveType = await _context.LeaveTypes
            .AsNoTracking()
            .SingleOrDefaultAsync(l =>
                l.LeaveTypeId == id.Value);

        if (leaveType == null)
        {
            return NotFound();
        }

        var model = new LeaveTypeEditViewModel
        {
            LeaveTypeId = leaveType.LeaveTypeId,
            Name = leaveType.Name,
            AllocationType = leaveType.AllocationType,
            AllocationLimit = leaveType.AllocationLimit,
            IsActive = leaveType.IsActive
        };

        return View(model);
    }

    // POST: /LeaveTypes/Edit/5
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        LeaveTypeEditViewModel model)
    {
        if (id != model.LeaveTypeId)
        {
            return BadRequest();
        }

        var leaveType = await _context.LeaveTypes
            .SingleOrDefaultAsync(l =>
                l.LeaveTypeId == id);

        if (leaveType == null)
        {
            return NotFound();
        }

        var normalizedName = model.Name?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "Leave type name is required.");
        }
        else
        {
            var duplicateExists = await _context.LeaveTypes
                .AnyAsync(l =>
                    l.LeaveTypeId != id &&
                    l.Name.ToLower() ==
                    normalizedName.ToLower());

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "A leave type with this name already exists.");
            }
        }

        ValidateAllocation(model.AllocationType, model.AllocationLimit);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        leaveType.Name = normalizedName!;
        leaveType.AllocationType = model.AllocationType;
        leaveType.AllocationLimit = model.AllocationLimit;
        leaveType.IsActive = model.IsActive;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                string.Empty,
                "The leave type could not be updated.");

            return View(model);
        }

        TempData["SuccessMessage"] =
            "Leave type updated successfully.";

        return RedirectToAction(nameof(Index));
    }


    private void ValidateAllocation(string? allocationType, decimal allocationLimit)
    {
        if (allocationType != "Annually" && allocationType != "Monthly")
        {
            ModelState.AddModelError(
                nameof(LeaveTypeCreateViewModel.AllocationType),
                "Allocation type must be Annually or Monthly.");
        }

        if (allocationLimit <= 0)
        {
            ModelState.AddModelError(
                nameof(LeaveTypeCreateViewModel.AllocationLimit),
                "Number of leaves must be greater than 0.");
        }
    }
}
