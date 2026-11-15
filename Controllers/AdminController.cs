using MenuMint.Data;
using MenuMint.Models;
using MenuMint.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MenuMint.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        string? name = null;
        try { name = (await _userManager.GetUserAsync(User))?.FullName; } catch { }
        ViewData["AdminName"] = string.IsNullOrWhiteSpace(name) ? "Platform Admin" : name;
        await next();
    }

    // ---------- DASHBOARD ----------
    public IActionResult Dashboard()
    {
        var all = DemoStore.Platform;
        var now = DateTime.UtcNow;

        var months = Enumerable.Range(0, 6)
            .Select(i => now.AddMonths(i - 5))
            .Select(d => new MonthBarVm
            {
                Label = d.ToString("MMM"),
                Count = all.Count(p => p.Joined.Year == d.Year && p.Joined.Month == d.Month)
            }).ToList();

        var vm = new AdminDashboardViewModel
        {
            Total = all.Count,
            Active = all.Count(p => p.Restaurant.IsActive),
            Inactive = all.Count(p => !p.Restaurant.IsActive),
            TotalItems = all.Sum(p => p.IsMain ? DemoStore.Items.Count : p.Items),
            Months = months,
            Recent = all.OrderByDescending(p => p.Joined).Take(5).Select(ToRow).ToList(),
            Largest = all.OrderByDescending(p => p.IsMain ? DemoStore.Items.Count : p.Items).Take(5).Select(ToRow).ToList(),
            Deactivated = all.Where(p => !p.Restaurant.IsActive).Select(ToRow).ToList(),
        };
        return View(vm);
    }

    // ---------- RESTAURANTS ----------
    public IActionResult Restaurants()
    {
        var rows = DemoStore.Platform.OrderByDescending(p => p.Joined).Select(ToRow).ToList();
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Toggle(int id)
    {
        var entry = DemoStore.Platform.FirstOrDefault(p => p.Restaurant.RestaurantId == id);
        if (entry is null) return NotFound();

        var r = entry.Restaurant;
        r.IsActive = !r.IsActive;

        // TODO (backend step): UPDATE Restaurants SET IsActive = @flag WHERE RestaurantId = @id
        TempData["Success"] = r.IsActive
            ? $"✅ \"{r.Name}\" is active again. Its public menu is back online."
            : $"⏸️ \"{r.Name}\" was deactivated. Its public menu is now offline.";

        return RedirectToAction(nameof(Restaurants));
    }

    private static AdminRowVm ToRow(PlatformEntry e) => new()
    {
        Id = e.Restaurant.RestaurantId,
        Name = e.Restaurant.Name,
        Slug = e.Restaurant.Slug,
        OwnerName = e.OwnerName,
        OwnerEmail = e.OwnerEmail,
        Items = e.IsMain ? DemoStore.Items.Count : e.Items,
        Categories = e.IsMain ? DemoStore.Categories.Count : e.Categories,
        Joined = e.Joined,
        IsActive = e.Restaurant.IsActive,
        IsMain = e.IsMain
    };
}