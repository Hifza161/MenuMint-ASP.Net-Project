using MenuMint.Models;

namespace MenuMint.Interfaces;

public interface ICategoryRepository
{
    /// <summary>All categories of a restaurant with their item counts, in display order.</summary>
    Task<List<CategorySummary>> GetSummariesAsync(int restaurantId);

    /// <summary>Plain category list (for dropdowns), in display order.</summary>
    Task<List<Category>> GetAllAsync(int restaurantId);

    Task<Category?> GetAsync(int categoryId, int restaurantId);

    Task<bool> NameExistsAsync(int restaurantId, string name, int? excludeCategoryId = null);

    /// <summary>Adds the category at the end of the list and returns its id.</summary>
    Task<int> CreateAsync(int restaurantId, string name);

    Task<bool> RenameAsync(int categoryId, int restaurantId, string name);
    Task<bool> SetActiveAsync(int categoryId, int restaurantId, bool isActive);

    /// <summary>Swaps the category with its neighbour above or below, inside a transaction.</summary>
    Task<bool> MoveAsync(int categoryId, int restaurantId, bool up);

    Task<DeleteCategoryResult> DeleteAsync(int categoryId, int restaurantId);
}