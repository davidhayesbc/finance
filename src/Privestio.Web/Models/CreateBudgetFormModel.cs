using System.ComponentModel.DataAnnotations;

namespace Privestio.Web.Models;

public class CreateBudgetFormModel
{
    [Required(ErrorMessage = "Category is required")]
    public string CategoryId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Year is required")]
    [Range(2000, 2100, ErrorMessage = "Year must be between 2000 and 2100")]
    public int Year { get; set; } = DateTime.Today.Year;

    [Required(ErrorMessage = "Month is required")]
    [Range(1, 12, ErrorMessage = "Month must be between 1 and 12")]
    public int Month { get; set; } = DateTime.Today.Month;

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, 999999999, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "CAD";

    public bool RolloverEnabled { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
