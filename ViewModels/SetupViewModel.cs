using System.ComponentModel.DataAnnotations;

namespace MenuMint.ViewModels;

public class SetupViewModel
{
    [Required(ErrorMessage = "Restaurant name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Restaurant name must be 2 to 100 characters.")]
    [Display(Name = "Restaurant name")]
    public string RestaurantName { get; set; } = string.Empty;
}