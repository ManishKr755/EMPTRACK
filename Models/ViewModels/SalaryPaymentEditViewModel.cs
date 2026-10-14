using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class SalaryPaymentEditViewModel
{
    public int PaymentId { get; set; }

    [Required]
    [Display(Name = "Employee")]
    public int EmployeeId { get; set; }

    [Required]
    [Display(Name = "Payment Date")]
    public DateOnly PaymentDate { get; set; }

    [Required]
    [Range(typeof(decimal), "0.01", "99999999.99")]
    [Display(Name = "Amount")]
    public decimal Amount { get; set; }
}