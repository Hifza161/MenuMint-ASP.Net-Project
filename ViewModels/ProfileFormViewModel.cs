using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MenuMint.ViewModels;

public class ProfileFormViewModel
{
    [Required(ErrorMessage = "Restaurant name is required.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2 to 100 characters.")]
    [Display(Name = "Restaurant name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(300, ErrorMessage = "Description can be up to 300 characters.")]
    public string? Description { get; set; }

    [StringLength(200, ErrorMessage = "Address can be up to 200 characters.")]
    public string? Address { get; set; }

    [RegularExpression(@"^[0-9+\-\s()]{7,20}$", ErrorMessage = "Enter a valid phone number.")]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    public IFormFile? LogoFile { get; set; }
    public bool RemoveLogo { get; set; }
    public string? ExistingLogoPath { get; set; }
    public IFormFile? CoverFile { get; set; }
    public bool RemoveCover { get; set; }
    public string? ExistingCoverPath { get; set; }
}