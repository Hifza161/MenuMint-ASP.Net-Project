using System.ComponentModel.DataAnnotations;

namespace MenuMint.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Your name is required.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Name must be 2 to 80 characters.")]
    [Display(Name = "Your name")]
    public string OwnerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Restaurant name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Restaurant name must be 2 to 100 characters.")]
    [Display(Name = "Restaurant name")]
    public string RestaurantName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "That doesn't look like a valid email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Use at least one letter and one number.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}