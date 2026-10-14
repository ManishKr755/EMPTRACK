namespace EMPTRACK.Models;

public class SalaryPayment
{
    public int PaymentId { get; set; }

    public int EmployeeId { get; set; }

    public DateOnly PaymentDate { get; set; }

    public decimal Amount { get; set; }

    // Navigation property
    public Employee Employee { get; set; } = null!;
}