using System.ComponentModel.DataAnnotations;

namespace Privestio.Web.Models;

public class CreateSinkingFundFormModel
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Target amount is required")]
    [Range(0.01, 999999999, ErrorMessage = "Target amount must be greater than zero")]
    public decimal TargetAmount { get; set; }

    [Required(ErrorMessage = "Due date is required")]
    public DateTime DueDate { get; set; } = DateTime.Today.AddMonths(6);

    public string Currency { get; set; } = "CAD";

    public string? AccountId { get; set; }

    public string? CategoryId { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
