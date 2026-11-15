using System.ComponentModel.DataAnnotations;
using MenuMint.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MenuMint.ViewModels;

public class ItemsPageViewModel
{
    public List<Category> Categories { get; set; } = new();
    public List<ItemCardVm> Items { get; set; } = new();
}

public class ItemCardVm
{
    public int MenuItemId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Emoji { get; set; } = "🍽️";
    public string? ImagePath { get; set; }
    public bool IsAvailable { get; set; }
}

public class ItemFormViewModel
{
    public int MenuItemId { get; set; }

    [Required(ErrorMessage = "Dish name is required.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Name must be 2 to 80 characters.")]
    [Display(Name = "Dish name")]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Price is required.")]
    [Range(1, 1000000, ErrorMessage = "Price must be between 1 and 1,000,000.")]
    public decimal? Price { get; set; }

    [StringLength(200, ErrorMessage = "Description can be up to 200 characters.")]
    public string? Description { get; set; }

    public string? Emoji { get; set; }
    public bool IsAvailable { get; set; } = true;

    public IFormFile? ImageFile { get; set; }
    public bool RemoveImage { get; set; }
    public string? ExistingImagePath { get; set; }

    [ValidateNever]
    public List<SelectListItem> Categories { get; set; } = new();
}