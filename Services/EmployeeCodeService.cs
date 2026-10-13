using EMPTRACK.Data;
using Microsoft.EntityFrameworkCore;

namespace EMPTRACK.Services;

public class EmployeeCodeService
{
    private readonly ApplicationDbContext _context;

    public EmployeeCodeService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // ROLE CODE
    // ============================================================

    public string GetRoleCode(string role)
    {
        return role switch
        {
            "Head Manager" => "2",
            "Manager" => "3",
            "HR Head" => "2",
            "HR" => "4",
            "Employee" => "4",

            // Admin is not an Employee.
            "Admin" => throw new ArgumentException(
                "Admin cannot be used as an Employee role."),

            _ => throw new ArgumentException(
                $"Unsupported employee role: {role}")
        };
    }

    // ============================================================
    // EMPLOYEE SERIAL
    // ============================================================

    public async Task<int> GenerateEmployeeSerialAsync()
    {
        // Find the highest existing EmployeeSerial.
        var highestSerial = await _context.Employees
            .Select(e => (int?)e.EmployeeSerial)
            .MaxAsync() ?? 0;

        var nextSerial = highestSerial + 1;

        if (nextSerial > 999)
        {
            throw new InvalidOperationException(
                "The maximum EmployeeSerial value of 999 has been reached.");
        }

        return nextSerial;
    }

    // ============================================================
    // EMPLOYEE CODE
    // ============================================================

    public string GenerateEmployeeCode(
        string departmentCode,
        string role,
        int employeeSerial)
    {
        if (string.IsNullOrWhiteSpace(departmentCode))
        {
            throw new ArgumentException(
                "Department code is required.");
        }

        if (employeeSerial < 1 || employeeSerial > 999)
        {
            throw new ArgumentOutOfRangeException(
                nameof(employeeSerial),
                "EmployeeSerial must be between 001 and 999.");
        }

        // Department code must represent exactly two digits.
        if (!int.TryParse(departmentCode, out var departmentNumber) ||
            departmentNumber < 0 ||
            departmentNumber > 99)
        {
            throw new ArgumentException(
                "Department code must be a two-digit numeric value.");
        }

        var departmentPart =
            departmentNumber.ToString("D2");

        var rolePart = GetRoleCode(role);

        var serialPart =
            employeeSerial.ToString("D3");

        var employeeCode =
            departmentPart +
            rolePart +
            serialPart;

        if (employeeCode.Length != 6 ||
            !employeeCode.All(char.IsDigit))
        {
            throw new InvalidOperationException(
                "Generated EmployeeCode is invalid.");
        }

        return employeeCode;
    }

    // ============================================================
    // EMPLOYEE CODE CHECK
    // ============================================================

    public async Task<bool> EmployeeCodeExistsAsync(
        string employeeCode,
        int? excludingEmployeeId = null)
    {
        var query = _context.Employees
            .AsNoTracking()
            .Where(e => e.EmployeeCode == employeeCode);

        if (excludingEmployeeId.HasValue)
        {
            query = query.Where(e =>
                e.EmployeeId != excludingEmployeeId.Value);
        }

        return await query.AnyAsync();
    }

    // ============================================================
    // EMPLOYEE CODE VALIDATION
    // ============================================================

    public bool IsValidEmployeeCode(
        string employeeCode,
        string departmentCode,
        string role,
        int employeeSerial)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            return false;
        }

        if (employeeCode.Length != 6)
        {
            return false;
        }

        if (!employeeCode.All(char.IsDigit))
        {
            return false;
        }

        var expectedCode = GenerateEmployeeCode(
            departmentCode,
            role,
            employeeSerial);

        return employeeCode == expectedCode;
    }
}