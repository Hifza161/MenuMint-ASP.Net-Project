using Dapper;
using MenuMint.Data;
using MenuMint.Interfaces;
using MenuMint.Models;
using MenuMint.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace MenuMint.Controllers;

// TEMPORARY: development self-test for the restaurant setup service. Deleted in Phase 10.
public class DiagnosticsSetupController : Controller
{
    private const string TempEmail = "diag-temp-owner@menumint.test";

    private readonly IRestaurantRepository _restaurants;
    private readonly ISlugService _realSlugs;
    private readonly IDbConnectionFactory _factory;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IWebHostEnvironment _env;

    public DiagnosticsSetupController(IRestaurantRepository restaurants, ISlugService realSlugs,
        IDbConnectionFactory factory, UserManager<ApplicationUser> users, IWebHostEnvironment env)
    {
        _restaurants = restaurants; _realSlugs = realSlugs; _factory = factory; _users = users; _env = env;
    }

    // Hands out a fixed list of slugs, then repeats the last one. Used to simulate a race.
    private class SequenceSlugService : ISlugService
    {
        private readonly Queue<string> _queue;
        private readonly string _last;
        public int Calls { get; private set; }

        public SequenceSlugService(params string[] slugs) { _queue = new Queue<string>(slugs); _last = slugs[^1]; }
        public string Slugify(string? text) => text ?? string.Empty;
        public Task<string> GenerateUniqueSlugAsync(string restaurantName)
        {
            Calls++;
            return Task.FromResult(_queue.Count > 0 ? _queue.Dequeue() : _last);
        }
    }

    private RestaurantSetupService Setup(ISlugService slugs) =>
        new(_restaurants, slugs, NullLogger<RestaurantSetupService>.Instance);

    [HttpGet("/diag/setup")]
    public async Task<IActionResult> Run()
    {
        if (!_env.IsDevelopment()) return NotFound();

        var admin = await _users.FindByEmailAsync("admin@menumint.com");
        if (admin is null) return Content("Admin user not found. Start the app once so the seeder creates it.");

        var log = new List<string>();
        void Check(string name, bool ok) => log.Add($"{(ok ? "PASS" : "FAIL")}  {name}");

        await CleanupAsync(admin.Id);
        try
        {
            // ---------- Normal creation ----------
            var id = await Setup(_realSlugs).CreateForOwnerAsync(admin.Id, "  Diag Setup Café  ");
            var made = await _restaurants.GetByIdAsync(id);
            Check("Creates a restaurant and returns its id", id > 0 && made is not null);
            Check("The name is trimmed", made?.Name == "Diag Setup Café");
            Check("The slug comes from the name (accents removed)", made?.Slug == "diag-setup-cafe");
            Check("The owner is linked", made?.OwnerUserId == admin.Id);
            Check("It starts active with empty profile fields",
                made is { IsActive: true, Description: "", Address: "", Phone: "" });

            // ---------- A second restaurant for the same owner is refused ----------
            var again = await TryAsync(() => Setup(_realSlugs).CreateForOwnerAsync(admin.Id, "Another One"));
            Check("A second restaurant for the same owner is refused (one per owner)", again is SqlException);
            await CleanupAsync(admin.Id);

            // ---------- Race: the slug gets taken between the check and the insert ----------
            var temp = new ApplicationUser { UserName = TempEmail, Email = TempEmail, FullName = "Diag Temp", EmailConfirmed = true };
            Check("A temporary second owner can be created", (await _users.CreateAsync(temp)).Succeeded);
            await _restaurants.CreateAsync(new Restaurant
            {
                OwnerUserId = temp.Id,
                Name = "Diag Race",
                Slug = "diag-race",
                Description = "",
                Address = "",
                Phone = "",
                IsActive = true
            });

            // Real slug service: sees the taken slug in advance and picks the next free one
            var real = await Setup(_realSlugs).CreateForOwnerAsync(admin.Id, "Diag Race");
            Check("Real slug service avoids a taken slug up front", (await _restaurants.GetByIdAsync(real))?.Slug == "diag-race-2");
            await DeleteRestaurantsAsync(admin.Id);

            // Stub: first answers with the TAKEN slug (the race), then with a free one
            var raceStub = new SequenceSlugService("diag-race", "diag-race-3");
            var raced = await Setup(raceStub).CreateForOwnerAsync(admin.Id, "Diag Race");
            Check("Race: the conflict is caught and the insert is retried", (await _restaurants.GetByIdAsync(raced))?.Slug == "diag-race-3");
            Check("Race: a new slug was requested exactly once more", raceStub.Calls == 2);
            await DeleteRestaurantsAsync(admin.Id);

            // Stub that always answers with the taken slug: must give up after 3 attempts
            var stuckStub = new SequenceSlugService("diag-race");
            var stuck = await TryAsync(() => Setup(stuckStub).CreateForOwnerAsync(admin.Id, "Diag Race"));
            Check("Gives up after 3 attempts with the database error", stuck is SqlException);
            Check("Exactly 3 attempts were made", stuckStub.Calls == 3);
            Check("Nothing was created for the owner after giving up", await _restaurants.GetByOwnerIdAsync(admin.Id) is null);
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

    private static async Task<Exception?> TryAsync(Func<Task> action)
    {
        try { await action(); return null; }
        catch (Exception ex) { return ex; }
    }

    private async Task DeleteRestaurantsAsync(string ownerUserId)
    {
        using var db = _factory.CreateConnection();
        await db.ExecuteAsync("DELETE FROM dbo.Restaurants WHERE OwnerUserId = @ownerUserId", new { ownerUserId });
    }

    private async Task CleanupAsync(string adminId)
    {
        var temp = await _users.FindByEmailAsync(TempEmail);

        using var db = _factory.CreateConnection();
        await db.ExecuteAsync(@"
            DELETE i FROM dbo.MenuItems i JOIN dbo.Restaurants r ON r.RestaurantId = i.RestaurantId
            WHERE r.OwnerUserId IN (@adminId, @tempId);
            DELETE FROM dbo.Restaurants WHERE OwnerUserId IN (@adminId, @tempId);",
            new { adminId, tempId = temp?.Id ?? "none" });

        if (temp is not null) await _users.DeleteAsync(temp);
    }
}