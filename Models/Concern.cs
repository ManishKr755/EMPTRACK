namespace EMPTRACK.Models;

using System.ComponentModel.DataAnnotations;

public class Concern
{
    public int ConcernId { get; set; }

    public int EmployeeId { get; set; }

    [Required]
    [StringLength(200)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Employee Employee { get; set; } = null!;

    public ICollection<ConcernMessage> Messages { get; set; }
        = new List<ConcernMessage>();
}
