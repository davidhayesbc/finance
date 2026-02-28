using System.ComponentModel.DataAnnotations;

namespace Privestio.Web.Models;

public class CreateTransactionFormModel
{
    [Required]
    public Guid AccountId { get; set; }

    [Required(ErrorMessage = "Date is required")]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be positive")]
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "CAD";

    [Required(ErrorMessage = "Description is required")]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Transaction type is required")]
    public string TransactionType { get; set; } = string.Empty;

    public Guid? CategoryId { get; set; }
    public Guid? PayeeId { get; set; }
    public string? Notes { get; set; }
}
