using System.ComponentModel.DataAnnotations;

namespace Privestio.Web.Models;

public class CreateAccountFormModel
{
    [Required(ErrorMessage = "Account name is required")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account type is required")]
    public string AccountType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Account sub-type is required")]
    public string AccountSubType { get; set; } = string.Empty;

    public string Currency { get; set; } = "CAD";
    public string? Institution { get; set; }
    public string? AccountNumber { get; set; }
    public decimal OpeningBalance { get; set; }
    public DateTime OpeningDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
}
