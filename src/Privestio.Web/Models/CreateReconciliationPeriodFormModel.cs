using System.ComponentModel.DataAnnotations;

namespace Privestio.Web.Models;

public class CreateReconciliationPeriodFormModel
{
    [Required(ErrorMessage = "Account is required")]
    public string AccountId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Statement date is required")]
    public DateTime? StatementDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Statement balance is required")]
    public decimal StatementBalanceAmount { get; set; }

    public string Currency { get; set; } = "CAD";

    [MaxLength(500)]
    public string? Notes { get; set; }
}
