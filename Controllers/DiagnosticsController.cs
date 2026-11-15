using Dapper;
using MenuMint.Data;
using MenuMint.Data.Repositories;
using MenuMint.Interfaces;
using MenuMint.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MenuMint.Controllers;

// TEMPORARY: development self-test for the repositories. Deleted in Phase 10.
public class DiagnosticsController : Controller
{
    private readonly IRestaurantRepository _restaurants;
    private readonly ICategoryRepository _categories;
    private readonly IDbConnectionFactory _factory;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IWebHostEnvironment _env;

    public DiagnosticsController(IRestaurantRepository restaurants, ICategoryRepository categories,
        IDbConnectionFactory factory, UserManager<ApplicationUser> users, IWebHostEnvironment env)
    {
        _restaurants = restaurants;
        _categories = categories;
        _factory = factory;
        _users = users;
        _env = env;
    }

    [HttpGet("/diag/repos")]
    public async Task<IActionResult> Repos()
    {
        if (!_env.IsDevelopment()) return NotFound();

        var admin = await _users.FindByEmailAsync("admin@menumint.com");
        if (admin is null) return Content("Admin user not found. Start the app once so the seeder creates it.");

        var log = new List<string>();
        void Check(string name, bool ok) => log.Add($"{(ok ? "PASS" : "FAIL")}  {name}");

        await CleanupAsync(admin.Id);
        try
        {
            // ---------- Restaurants ----------
            var rid = await _restaurants.CreateAsync(new Restaurant
            {
                OwnerUserId = admin.Id,
                Name = "Diag Test",
                Slug = "diag-test",
                Description = "first",
                Address = "",
                Phone = "",
                IsActive = true
            });
            Check("Restaurant created and id returned", rid > 0);

            var byId = await _restaurants.GetByIdAsync(rid);
            Check("GetById returns the restaurant", byId?.Name == "Diag Test");
            Check("GetBySlug finds it", (await _restaurants.GetBySlugAsync("diag-test"))?.RestaurantId == rid);
            Check("GetByOwnerId finds it", (await _restaurants.GetByOwnerIdAsync(admin.Id))?.RestaurantId == rid);
            Check("SlugExists true for an existing slug", await _restaurants.SlugExistsAsync("diag-test"));
            Check("SlugExists false for a free slug", !await _restaurants.SlugExistsAsync("no-such-slug"));

            byId!.Description = "second"; byId.Phone = "0300 1234567";
            Check("UpdateProfile returns true", await _restaurants.UpdateProfileAsync(byId));
            var updated = await _restaurants.GetByIdAsync(rid);
            Check("UpdateProfile saved the new values", updated?.Description == "second" && updated.Phone == "0300 1234567");
            Check("Slug unchanged after update", updated?.Slug == "diag-test");

            await _restaurants.SetActiveAsync(rid, false);
            Check("SetActive(false)", (await _restaurants.GetByIdAsync(rid))?.IsActive == false);
            await _restaurants.SetActiveAsync(rid, true);
            Check("SetActive(true)", (await _restaurants.GetByIdAsync(rid))?.IsActive == true);

            await _restaurants.MarkQrGeneratedAsync(rid);
            Check("MarkQrGenerated sets the date", (await _restaurants.GetByIdAsync(rid))?.QrGeneratedAt != null);

            // ---------- Categories ----------
            var c1 = await _categories.CreateAsync(rid, "Starters");
            var c2 = await _categories.CreateAsync(rid, "Mains");
            var c3 = await _categories.CreateAsync(rid, "Desserts");
            string Names(IEnumerable<CategorySummary> s) => string.Join(",", s.Select(x => x.Name));

            var list = await _categories.GetSummariesAsync(rid);
            Check("Three categories created in order", Names(list) == "Starters,Mains,Desserts");
            Check("Item counts are zero", list.All(x => x.ItemCount == 0));
            Check("GetAll returns three", (await _categories.GetAllAsync(rid)).Count == 3);

            Check("NameExists true for an existing name", await _categories.NameExistsAsync(rid, "Starters"));
            Check("NameExists false for a new name", !await _categories.NameExistsAsync(rid, "Nope"));
            Check("NameExists ignores the category being edited", !await _categories.NameExistsAsync(rid, "Starters", c1));

            Check("Rename returns true", await _categories.RenameAsync(c2, rid, "Main Course"));
            Check("Rename saved", (await _categories.GetAsync(c2, rid))?.Name == "Main Course");

            Check("Move down/up returns true", await _categories.MoveAsync(c3, rid, up: true));
            Check("Move swapped the order", Names(await _categories.GetSummariesAsync(rid)) == "Starters,Desserts,Main Course");
            Check("Moving the first one up is a harmless no-op", await _categories.MoveAsync(c1, rid, up: true));
            Check("Order unchanged after the no-op", Names(await _categories.GetSummariesAsync(rid)) == "Starters,Desserts,Main Course");

            await _categories.SetActiveAsync(c3, rid, false);
            Check("SetActive(false) saved", (await _categories.GetAsync(c3, rid))?.IsActive == false);

            // ---------- Scoping: another restaurant must not reach these rows ----------
            var other = rid + 9999;
            Check("SCOPE: Get with a wrong restaurant returns nothing", await _categories.GetAsync(c1, other) is null);
            Check("SCOPE: Rename with a wrong restaurant fails", !await _categories.RenameAsync(c1, other, "Hacked"));
            Check("SCOPE: Move with a wrong restaurant fails", !await _categories.MoveAsync(c1, other, up: false));
            Check("SCOPE: Delete with a wrong restaurant is NotFound", await _categories.DeleteAsync(c1, other) == DeleteCategoryResult.NotFound);
            Check("SCOPE: the name was not changed", (await _categories.GetAsync(c1, rid))?.Name == "Starters");

            // ---------- Delete ----------
            Check("Delete returns Deleted", await _categories.DeleteAsync(c1, rid) == DeleteCategoryResult.Deleted);
            Check("Two categories remain", (await _categories.GetSummariesAsync(rid)).Count == 2);
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

    // Removes everything the test created for this owner
    private async Task CleanupAsync(string ownerUserId)
    {
        using var db = _factory.CreateConnection();
        await db.ExecuteAsync(@"
            DELETE i FROM dbo.MenuItems i JOIN dbo.Restaurants r ON r.RestaurantId = i.RestaurantId WHERE r.OwnerUserId = @ownerUserId;
            DELETE FROM dbo.Restaurants WHERE OwnerUserId = @ownerUserId;", new { ownerUserId });
    }
}