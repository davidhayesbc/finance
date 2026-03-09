using System.ComponentModel.DataAnnotations;

namespace Privestio.Web.Models;

public class CreateForecastScenarioFormModel
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }
}
