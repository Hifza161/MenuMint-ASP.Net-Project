namespace MenuMint.Interfaces;

public interface IRestaurantSetupService
{
    /// <summary>
    /// Creates the owner's restaurant with a unique slug and returns its id.
    /// If another registration takes the same slug at the very same moment, it tries again with a new one.
    /// </summary>
    Task<int> CreateForOwnerAsync(string ownerUserId, string restaurantName);
}