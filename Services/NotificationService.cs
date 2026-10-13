using EMPTRACK.Data;
using EMPTRACK.Models;
using Microsoft.EntityFrameworkCore;

namespace EMPTRACK.Services;

public class NotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Notification> CreateNotificationAsync(
        int employeeId,
        string title,
        string message)
    {
        if (employeeId <= 0)
        {
            throw new ArgumentException(
                "A valid employee is required.",
                nameof(employeeId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Notification title is required.",
                nameof(title));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException(
                "Notification message is required.",
                nameof(message));
        }

        var employeeExists = await _context.Employees
            .AsNoTracking()
            .AnyAsync(e => e.EmployeeId == employeeId);

        if (!employeeExists)
        {
            throw new ArgumentException(
                "The selected employee does not exist.",
                nameof(employeeId));
        }

        var notification = new Notification
        {
            EmployeeId = employeeId,
            Title = title.Trim(),
            Message = message.Trim(),
            IsRead = false,
            CreatedAt = DateTime.Now
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return notification;
    }
}
