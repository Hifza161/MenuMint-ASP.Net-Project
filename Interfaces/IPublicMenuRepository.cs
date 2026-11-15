using MenuMint.Models;

namespace MenuMint.Interfaces
{
    public interface IPublicMenuRepository
    {
        /// <summary>
        /// Everything the public page needs, in one round trip. Null if the slug is unknown.
        /// An inactive restaurant is returned with no sections, so its menu never leaks.
        /// Hidden categories and categories without items are left out.
        /// </summary>
        Task<PublicMenu?> GetBySlugAsync(string slug);
        
    }
}
