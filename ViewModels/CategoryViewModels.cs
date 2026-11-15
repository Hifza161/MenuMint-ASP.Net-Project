using System.ComponentModel.DataAnnotations;

namespace MenuMint.ViewModels;

public class CategoryRowVm
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public bool IsActive { get; set; }
}

public class CategoryFormViewModel
{
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Category name is required.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Category name must be 2 to 50 characters.")]
    public string Name { get; set; } = string.Empty;
}