using Dapper;
using MenuMint.Data;
using MenuMint.Interfaces;
using MenuMint.Interfaces;
using MenuMint.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MenuMint.Controllers;

// TEMPORARY: development self-test for items, public menu and admin repositories. Deleted in Phase 10.
public class DiagnosticsItemsController : Controller
{
    private readonly IRestaurantRepository _restaurants;
    private readonly ICategoryRepository _categories;
    private readonly IMenuItemRepository _items;
    private readonly IPublicMenuRepository _public;
    private readonly IAdminRepository _admin;
    private readonly IDbConnectionFactory _factory;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IWebHostEnvironment _env;

    public DiagnosticsItemsController(IRestaurantRepository restaurants, ICategoryRepository categories,
        IMenuItemRepository items, IPublicMenuRepository publicMenu, IAdminRepository admin,
        IDbConnectionFactory factory, UserManager<ApplicationUser> users, IWebHostEnvironment env)
    {
        _restaurants = restaurants; _categories = categories; _items = items;
        _public = publicMenu; _admin = admin; _factory = factory; _users = users; _env = env;
    }

    [HttpGet("/diag/repos2")]
    public async Task<IActionResult> Repos2()
    {
        if (!_env.IsDevelopment()) return NotFound();

        var admin = await _users.FindByEmailAsync("admin@menumint.com");
        if (admin is null) return Content("Admin user not found. Start the app once so the seeder creates it.");

        var log = new List<string>();
        void Check(string name, bool ok) => log.Add($"{(ok ? "PASS" : "FAIL")}  {name}");
        string Names(IEnumerable<MenuItemRow> r) => string.Join(",", r.Select(x => x.Name));

        await CleanupAsync(admin.Id);
        try
        {
            var rid = await _restaurants.CreateAsync(new Restaurant
            {
                OwnerUserId = admin.Id,
                Name = "Diag Menu",
                Slug = "diag-menu",
                Description = "",
                Address = "",
                Phone = "",
                IsActive = true
            });
            var cStart = await _categories.CreateAsync(rid, "Starters");
            var cMain = await _categories.CreateAsync(rid, "Mains");
            var cHide = await _categories.CreateAsync(rid, "Hidden");
            var other = rid + 9999;

            MenuItem New(int cat, string name, decimal price, bool available = true, string? image = null) => new()
            {
                RestaurantId = rid,
                CategoryId = cat,
                Name = name,
                Description = name + " description",
                Price = price,
                Emoji = "🍽️",
                ImagePath = image,
                IsAvailable = available
            };

            // ---------- Items: create and read ----------
            var i1 = await _items.CreateAsync(New(cStart, "Bruschetta", 650, true, "/images/dishes/bruschetta.jpg"));
            var i2 = await _items.CreateAsync(New(cMain, "Pizza", 1450));
            var i3 = await _items.CreateAsync(New(cMain, "Burger", 1250, available: false));
            var i4 = await _items.CreateAsync(New(cHide, "Secret", 99.50m));
            Check("Four items created with ids", new[] { i1, i2, i3, i4 }.All(x => x > 0));
            Check("SCOPE: Create with a category of another restaurant is refused",
                await _items.CreateAsync(new MenuItem { RestaurantId = other, CategoryId = cStart, Name = "Sneaky", Price = 10 }) == 0);

            var rows = await _items.GetByRestaurantAsync(rid);
            Check("List is ordered by category, then name", Names(rows) == "Bruschetta,Burger,Pizza,Secret");
            Check("List includes the category name", rows.First().CategoryName == "Starters");
            Check("Decimal price survives a round trip (99.50)", rows.Last().Price == 99.50m);

            var one = await _items.GetAsync(i1, rid);
            Check("Get returns the item", one?.Name == "Bruschetta" && one.ImagePath == "/images/dishes/bruschetta.jpg");
            Check("SCOPE: Get with a wrong restaurant returns nothing", await _items.GetAsync(i1, other) is null);

            // ---------- Items: update ----------
            one!.Name = "Bruschetta Deluxe"; one.Price = 700; one.CategoryId = cMain; one.ImagePath = null;
            Check("Update returns true", await _items.UpdateAsync(one));
            var after = await _items.GetAsync(i1, rid);
            Check("Update saved name, price and category, and cleared the image",
                after is { Name: "Bruschetta Deluxe", Price: 700 } && after.CategoryId == cMain && after.ImagePath is null);

            after!.CategoryId = 0;
            Check("Update to a category that does not exist is refused", !await _items.UpdateAsync(after));
            after.CategoryId = cStart; after.RestaurantId = other;
            Check("SCOPE: Update with a wrong restaurant fails", !await _items.UpdateAsync(after));

            // put it back so the later expectations stay simple
            after.RestaurantId = rid; after.Name = "Bruschetta"; after.Price = 650;
            after.ImagePath = "/images/dishes/bruschetta.jpg";
            Check("Update back to the original values", await _items.UpdateAsync(after));

            // ---------- Items: toggle, counts, recent ----------
            var t1 = await _items.ToggleAvailabilityAsync(i2, rid);
            Check("Toggle makes it sold out and returns the name", t1 is { IsAvailable: false, Name: "Pizza" });
            var t2 = await _items.ToggleAvailabilityAsync(i2, rid);
            Check("Toggle again makes it available", t2 is { IsAvailable: true });
            Check("SCOPE: Toggle with a wrong restaurant returns nothing", await _items.ToggleAvailabilityAsync(i2, other) is null);

            var counts = await _items.GetCountsAsync(rid);
            Check("Counts: 4 total, 3 available, 1 sold out", counts is { Total: 4, Available: 3, SoldOut: 1 });
            Check("Recent returns the newest two first", Names(await _items.GetRecentAsync(rid, 2)) == "Secret,Burger");

            // ---------- Categories now see the items ----------
            var sums = await _categories.GetSummariesAsync(rid);
            Check("Category item counts reflect the items",
                string.Join(",", sums.Select(s => $"{s.Name}:{s.ItemCount}")) == "Starters:1,Mains:2,Hidden:1");
            Check("Deleting a category that has items is refused",
                await _categories.DeleteAsync(cMain, rid) == DeleteCategoryResult.HasItems);

            // ---------- Public menu ----------
            await _categories.SetActiveAsync(cHide, rid, false);
            var menu = await _public.GetBySlugAsync("diag-menu");
            Check("Public menu is found by slug", menu?.Restaurant.RestaurantId == rid);
            Check("Hidden category is left out",
                string.Join(",", menu!.Sections.Select(s => s.Category.Name)) == "Starters,Mains");
            Check("Items are ordered available first, then by name",
                string.Join(",", menu.Sections[1].Items.Select(i => i.Name)) == "Pizza,Burger");
            Check("Slug lookup ignores case", await _public.GetBySlugAsync("DIAG-MENU") is not null);
            Check("Unknown slug returns null", await _public.GetBySlugAsync("no-such-menu") is null);

            await _restaurants.SetActiveAsync(rid, false);
            var off = await _public.GetBySlugAsync("diag-menu");
            Check("Deactivated restaurant is found but marked inactive", off is { Restaurant.IsActive: false });
            Check("Deactivated restaurant exposes no categories or items", off!.Sections.Count == 0);
            await _restaurants.SetActiveAsync(rid, true);

            // ---------- Admin ----------
            var stats = await _admin.GetStatsAsync();
            Check("Stats: at least 1 restaurant, and active + inactive = total",
                stats.Total >= 1 && stats.Active + stats.Inactive == stats.Total);
            Check("Stats: item total includes the 4 test items", stats.TotalItems >= 4);

            var all = await _admin.GetRestaurantsAsync();
            var mine = all.FirstOrDefault(r => r.Restaurant.RestaurantId == rid);
            Check("Admin list has the restaurant with its owner's name and email",
                mine is not null && mine.OwnerEmail == "admin@menumint.com" && mine.OwnerName == admin.FullName);
            Check("Admin list shows 4 items and 3 categories", mine is { ItemCount: 4, CategoryCount: 3 });
            Check("Admin list is newest first",
                all.Zip(all.Skip(1), (a, b) => a.Restaurant.CreatedAt >= b.Restaurant.CreatedAt).All(x => x));

            var recent = await _admin.GetRecentAsync(3);
            Check("Recent returns between 1 and 3 restaurants", recent.Count is >= 1 and <= 3);

            var largest = await _admin.GetLargestAsync(100);
            Check("Largest menus are sorted by item count and include the test restaurant",
                largest.Zip(largest.Skip(1), (a, b) => a.ItemCount >= b.ItemCount).All(x => x)
                && largest.Any(r => r.Restaurant.RestaurantId == rid));

            await _restaurants.SetActiveAsync(rid, false);
            Check("Inactive list contains the deactivated restaurant",
                (await _admin.GetInactiveAsync()).Any(r => r.Restaurant.RestaurantId == rid));
            await _restaurants.SetActiveAsync(rid, true);
            Check("Inactive list no longer contains it after reactivation",
                !(await _admin.GetInactiveAsync()).Any(r => r.Restaurant.RestaurantId == rid));

            var months = await _admin.GetMonthlySignupsAsync(6);
            var now = DateTime.UtcNow;
            Check("Monthly signups: 6 months in ascending order",
                months.Count == 6 && months.Zip(months.Skip(1), (a, b) => a.Year * 12 + a.Month < b.Year * 12 + b.Month).All(x => x));
            Check("Monthly signups: this month includes the test restaurant",
                months.Last().Year == now.Year && months.Last().Month == now.Month && months.Last().Count >= 1);

            // ---------- Delete ----------
            Check("SCOPE: Delete with a wrong restaurant removes nothing",
                await _items.DeleteAsync(i4, other) is null && await _items.GetAsync(i4, rid) is not null);
            var deleted = await _items.DeleteAsync(i4, rid);
            Check("Delete returns the name and the image path", deleted is { Name: "Secret", ImagePath: null });
            Check("The deleted item is gone", await _items.GetAsync(i4, rid) is null);
            Check("Deleting it again returns nothing", await _items.DeleteAsync(i4, rid) is null);
            Check("An emptied category can now be deleted",
                await _categories.DeleteAsync(cHide, rid) == DeleteCategoryResult.Deleted);
        }
        catch (Exception ex)
        {
            log.Add($"FAIL  Unexpected exception: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            await CleanupAsync(admin.Id);
        }

        var failed = log.Count(l => l.StartsWith("FAIL"));
        var summary = failed == 0 ? $"ALL {log.Count} CHECKS PASSED" : $"{failed} OF {log.Count} CHECKS FAILED";
        return Content(summary + "\n\n" + string.Join("\n", log) + "\n\n(Test data was removed.)", "text/plain");
    }

    private async Task CleanupAsync(string ownerUserId)
    {
        using var db = _factory.CreateConnection();
        await db.ExecuteAsync(@"
            DELETE i FROM dbo.MenuItems i JOIN dbo.Restaurants r ON r.RestaurantId = i.RestaurantId WHERE r.OwnerUserId = @ownerUserId;
            DELETE FROM dbo.Restaurants WHERE OwnerUserId = @ownerUserId;", new { ownerUserId });
    }
}