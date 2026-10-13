using System.ComponentModel.DataAnnotations;

namespace EMPTRACK.Models.ViewModels;

public class AuditLogIndexViewModel
{
    public List<AuditLogListItemViewModel> Logs { get; set; }
        = new();

    public string? Search { get; set; }

    public string? Action { get; set; }

    [Display(Name = "From Date")]
    public DateTime? FromDate { get; set; }

    [Display(Name = "To Date")]
    public DateTime? ToDate { get; set; }
}

public class AuditLogListItemViewModel
{
    public int AuditLogId { get; set; }

    public int UserAccountId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Target { get; set; } = string.Empty;

    public DateTime DateTime { get; set; }

    public string Description { get; set; } = string.Empty;
}