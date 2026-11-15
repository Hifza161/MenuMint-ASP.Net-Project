using Dapper;
using MenuMint.Data;
using MenuMint.Interfaces;
using MenuMint.Models;
using MenuMint.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace MenuMint.Controllers;

// TEMPORARY: development self-test for the services. Deleted in Phase 10.
public class DiagnosticsServicesController : Controller
{
    private readonly ISlugService _slugs;
    private readonly IImageStorageService _images;
    private readonly IRestaurantRepository _restaurants;
    private readonly IDbConnectionFactory _factory;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IWebHostEnvironment _env;

    public DiagnosticsServicesController(ISlugService slugs, IImageStorageService images,
        IRestaurantRepository restaurants, IDbConnectionFactory factory,
        UserManager<ApplicationUser> users, IWebHostEnvironment env)
    {
        _slugs = slugs; _images = images; _restaurants = restaurants;
        _factory = factory; _users = users; _env = env;
    }

    private class FakeAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }

    private static IFormFile MakeFile(byte[] bytes, string fileName) =>
        new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName);

    private static byte[] Jpeg(int size = 100) { var b = new byte[size]; new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }.CopyTo(b, 0); return b; }
    private static byte[] Png(int size = 100) { var b = new byte[size]; new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(b, 0); return b; }
    private static byte[] Webp(int size = 100) { var b = new byte[size]; Encoding.ASCII.GetBytes("RIFF").CopyTo(b, 0); Encoding.ASCII.GetBytes("WEBP").CopyTo(b, 8); return b; }

    private string Full(string webPath) =>
        Path.Combine(_env.WebRootPath, webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

    [HttpGet("/diag/services")]
    public async Task<IActionResult> Services()
    {
        if (!_env.IsDevelopment()) return NotFound();

        var admin = await _users.FindByEmailAsync("admin@menumint.com");
        if (admin is null) return Content("Admin user not found. Start the app once so the seeder creates it.");

        var log = new List<string>();
        void Check(string name, bool ok) => log.Add($"{(ok ? "PASS" : "FAIL")}  {name}");

        string? p1 = null, p2 = null, p3 = null;
        await CleanupAsync(admin.Id);
        try
        {
            // ---------- Slugify (no database) ----------
            Check("Slugify: 'Saffron & Salt' -> saffron-and-salt", _slugs.Slugify("Saffron & Salt") == "saffron-and-salt");
            Check("Slugify: accents are removed (Café -> cafe)", _slugs.Slugify("  Brew & Bite Café  ") == "brew-and-bite-cafe");
            Check("Slugify: repeated spaces, hyphens and symbols collapse", _slugs.Slugify("Karachi   Biryani--House!!") == "karachi-biryani-house");
            Check("Slugify: apostrophes are dropped", _slugs.Slugify("Tom's Pizza") == "toms-pizza");
            Check("Slugify: '100% Halal' -> 100-halal", _slugs.Slugify("100% Halal") == "100-halal");
            Check("Slugify: only hyphens gives an empty string", _slugs.Slugify("---") == "");
            Check("Slugify: null, empty and whitespace give an empty string",
                _slugs.Slugify(null) == "" && _slugs.Slugify("") == "" && _slugs.Slugify("   ") == "");
            Check("Slugify: a name written only in Urdu gives an empty string",
                _slugs.Slugify("\u06A9\u06BE\u0627\u0646\u0627") == "");

            // ---------- Unique slugs (database) ----------
            var rid = await _restaurants.CreateAsync(new Restaurant
            {
                OwnerUserId = admin.Id,
                Name = "Diag Slug",
                Slug = "diag-slug",
                Description = "",
                Address = "",
                Phone = "",
                IsActive = true
            });
            Check("Unique slug: a taken slug gets the suffix -2", await _slugs.GenerateUniqueSlugAsync("Diag Slug") == "diag-slug-2");
            var free = await _slugs.GenerateUniqueSlugAsync("Totally Free Name Xyz");
            Check("Unique slug: a free name is used as it is", free == "totally-free-name-xyz");
            Check("Unique slug: the result is really not taken", !await _restaurants.SlugExistsAsync(free));
            Check("Unique slug: an unusable name falls back to 'restaurant'",
                (await _slugs.GenerateUniqueSlugAsync("!!!")).StartsWith("restaurant"));
            Check("Unique slug: whitespace falls back to 'restaurant'",
                (await _slugs.GenerateUniqueSlugAsync("   ")).StartsWith("restaurant"));
            Check("Unique slug: a 150-character name is cut to 100",
                (await _slugs.GenerateUniqueSlugAsync(new string('a', 150))).Length == 100);

            // ---------- Image validation ----------
            Check("Image: a valid JPEG passes", await _images.ValidateAsync(MakeFile(Jpeg(), "dish.jpg"), ImageFolder.Items) is null);
            Check("Image: a valid PNG passes", await _images.ValidateAsync(MakeFile(Png(), "dish.png"), ImageFolder.Items) is null);
            Check("Image: a valid WEBP passes", await _images.ValidateAsync(MakeFile(Webp(), "dish.webp"), ImageFolder.Items) is null);

            var fake = await _images.ValidateAsync(
                MakeFile(Encoding.UTF8.GetBytes("<script>alert(1)</script> padding padding"), "evil.jpg"), ImageFolder.Items);
            Check("Image: a script renamed to .jpg is rejected", fake == "Only JPG, PNG or WEBP images are allowed.");

            Check("Image: an empty file is rejected",
                await _images.ValidateAsync(MakeFile(Array.Empty<byte>(), "e.jpg"), ImageFolder.Items) == "The file is empty.");
            Check("Image: items over 2 MB are rejected",
                await _images.ValidateAsync(MakeFile(Jpeg(2 * 1024 * 1024 + 1), "big.jpg"), ImageFolder.Items) == "Image must be 2 MB or smaller.");
            Check("Image: logos over 2 MB are rejected with their own wording",
                await _images.ValidateAsync(MakeFile(Jpeg(2 * 1024 * 1024 + 1), "big.jpg"), ImageFolder.Logos) == "Logo must be 2 MB or smaller.");
            Check("Image: a cover of 2 MB + 1 byte is still allowed (limit is 3 MB)",
                await _images.ValidateAsync(MakeFile(Jpeg(2 * 1024 * 1024 + 1), "cover.jpg"), ImageFolder.Covers) is null);
            Check("Image: covers over 3 MB are rejected",
                await _images.ValidateAsync(MakeFile(Jpeg(3 * 1024 * 1024 + 1), "big.jpg"), ImageFolder.Covers) == "Cover photo must be 3 MB or smaller.");

            // ---------- Image save and delete ----------
            p1 = await _images.SaveAsync(MakeFile(Jpeg(), "photo.jpg"), ImageFolder.Items);
            Check("Save: an item image gets an /uploads/items/ path ending in .jpg", p1.StartsWith("/uploads/items/") && p1.EndsWith(".jpg"));
            Check("Save: the file exists on disk", System.IO.File.Exists(Full(p1)));

            p2 = await _images.SaveAsync(MakeFile(Png(), "pretending.jpg"), ImageFolder.Logos);
            Check("Save: the extension comes from the content, not the file name", p2.StartsWith("/uploads/logos/") && p2.EndsWith(".png"));

            p3 = await _images.SaveAsync(MakeFile(Webp(), "cover.png"), ImageFolder.Covers);
            Check("Save: a cover goes to /uploads/covers/ as .webp", p3.StartsWith("/uploads/covers/") && p3.EndsWith(".webp"));

            var again = await _images.SaveAsync(MakeFile(Jpeg(), "photo.jpg"), ImageFolder.Items);
            Check("Save: the same file name twice never collides", again != p1);
            _images.Delete(again);

            _images.Delete(p1);
            Check("Delete: removes the uploaded file", !System.IO.File.Exists(Full(p1)));

            var demo = Path.Combine(_env.WebRootPath, "images", "dishes", "lava-cake.jpg");
            var demoExisted = System.IO.File.Exists(demo);
            _images.Delete("/images/dishes/lava-cake.jpg");
            Check("SAFETY: demo images outside /uploads are never deleted", System.IO.File.Exists(demo) == demoExisted);

            var appSettings = Path.Combine(_env.ContentRootPath, "appsettings.json");
            _images.Delete("/uploads/items/../../../appsettings.json");
            Check("SAFETY: a path-traversal attempt cannot delete other files", System.IO.File.Exists(appSettings));

            _images.Delete(null); _images.Delete(""); _images.Delete("/uploads/items/does-not-exist.jpg");
            Check("Delete: null, empty and missing files are ignored without errors", true);

            // ---------- Current restaurant (fake logged-in user) ----------
            ICurrentRestaurantService ServiceFor(string? userId)
            {
                var ctx = new DefaultHttpContext();
                if (userId is not null)
                    ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "test"));
                return new CurrentRestaurantService(new FakeAccessor { HttpContext = ctx }, _restaurants);
            }

            var current = ServiceFor(admin.Id);
            Check("Current: UserId is read from the logged-in user", current.UserId == admin.Id);
            Check("Current: finds the owner's restaurant", (await current.GetAsync())?.RestaurantId == rid);
            Check("Current: GetIdAsync returns the id", await current.GetIdAsync() == rid);

            var row = (await _restaurants.GetByIdAsync(rid))!;
            row.Name = "Renamed in DB";
            await _restaurants.UpdateProfileAsync(row);
            Check("Current: the restaurant is cached for the rest of the request", (await current.GetAsync())?.Name == "Diag Slug");
            current.Invalidate();
            Check("Current: Invalidate loads fresh data", (await current.GetAsync())?.Name == "Renamed in DB");

            Check("Current: a visitor who is not logged in gets null", await ServiceFor(null).GetAsync() is null);
            Check("Current: a user without a restaurant gets null", await ServiceFor("no-such-user-id").GetAsync() is null);
            Check("Current: GetIdAsync is null when there is no restaurant", await ServiceFor("no-such-user-id").GetIdAsync() is null);
        }
        catch (Exception ex)
        {
            log.Add($"FAIL  Unexpected exception: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _images.Delete(p1); _images.Delete(p2); _images.Delete(p3);
            await CleanupAsync(admin.Id);
        }

        var failed = log.Count(l => l.StartsWith("FAIL"));
        var summary = failed == 0 ? $"ALL {log.Count} CHECKS PASSED" : $"{failed} OF {log.Count} CHECKS FAILED";
        return Content(summary + "\n\n" + string.Join("\n", log) + "\n\n(Test data and test images were removed.)", "text/plain");
    }

    private async Task CleanupAsync(string ownerUserId)
    {
        using var db = _factory.CreateConnection();
        await db.ExecuteAsync(@"
            DELETE i FROM dbo.MenuItems i JOIN dbo.Restaurants r ON r.RestaurantId = i.RestaurantId WHERE r.OwnerUserId = @ownerUserId;
            DELETE FROM dbo.Restaurants WHERE OwnerUserId = @ownerUserId;", new { ownerUserId });
    }
}