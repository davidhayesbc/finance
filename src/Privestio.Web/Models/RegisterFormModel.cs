using System.ComponentModel.DataAnnotations;

namespace Privestio.Web.Models;

public class RegisterFormModel
{
    [Required(ErrorMessage = "Display name is required")]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(254, ErrorMessage = "Email address must be 254 characters or fewer")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(12, ErrorMessage = "Password must be at least 12 characters")]
    public string Password { get; set; } = string.Empty;
}
