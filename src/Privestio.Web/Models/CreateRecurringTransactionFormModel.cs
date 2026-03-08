using System.ComponentModel.DataAnnotations;

namespace Privestio.Web.Models;

public class CreateRecurringTransactionFormModel
{
    [Required(ErrorMessage = "Account is required")]
    public string AccountId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, 999999999, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Transaction type is required")]
    public string TransactionType { get; set; } = "Expense";

    [Required(ErrorMessage = "Frequency is required")]
    public string Frequency { get; set; } = "Monthly";

    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime? EndDate { get; set; }

    public string Currency { get; set; } = "CAD";

    public string? CategoryId { get; set; }

    public string? PayeeId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
