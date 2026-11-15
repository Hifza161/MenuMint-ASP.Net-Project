using MenuMint.Data;
using MenuMint.Interfaces;
using MenuMint.Models;
using MenuMint.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MenuMint.Controllers;

[Authorize(Roles = "Owner")]
public class OwnerController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;
    private readonly ICurrentRestaurantService _current;
    private readonly IRestaurantSetupService _setup;

    public OwnerController(UserManager<ApplicationUser> userManager, IWebHostEnvironment env,
        ICurrentRestaurantService current, IRestaurantSetupService setup)
    {
        _userManager = userManager;
        _env = env;
        _current = current;
        _setup = setup;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // An owner without a restaurant can only reach the Setup page
        var action = context.RouteData.Values["action"]?.ToString();
        if (!string.Equals(action, nameof(Setup), StringComparison.OrdinalIgnoreCase)
            && await _current.GetIdAsync() is null)
        {
            context.Result = RedirectToAction(nameof(Setup));
            return;
        }

        var user = await _userManager.GetUserAsync(User);
        ViewData["OwnerName"] = user?.FullName ?? "Owner";

        // TODO (Phase 5): read these from the real restaurant. Until then the owner pages show the demo restaurant.
        ViewData["RestaurantName"] = DemoStore.Restaurant.Name;
        ViewData["Slug"] = DemoStore.Restaurant.Slug;

        await next();
    }

    // ---------- SETUP (owner has an account but no restaurant yet) ----------
    [HttpGet]
    public async Task<IActionResult> Setup()
    {
        if (await _current.GetIdAsync() is not null) return RedirectToAction(nameof(Dashboard));
        return View(new SetupViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Setup(SetupViewModel model)
    {
        if (await _current.GetIdAsync() is not null) return RedirectToAction(nameof(Dashboard));
        if (!ModelState.IsValid) return View(model);

        await _setup.CreateForOwnerAsync(_current.UserId!, model.RestaurantName);
        _current.Invalidate();

        TempData["Success"] = "🎉 Your restaurant is ready.";
        return RedirectToAction(nameof(Dashboard));
    }

    // ---------- DASHBOARD ----------
    public IActionResult Dashboard()
    {
        var cats = DemoStore.Categories;
        var items = DemoStore.Items;

        var vm = new DashboardViewModel
        {
            OwnerName = (string)ViewData["OwnerName"]!,
            RestaurantName = (string)ViewData["RestaurantName"]!,
            Slug = (string)ViewData["Slug"]!,
            CategoryCount = cats.Count,
            ItemCount = items.Count,
            AvailableCount = items.Count(i => i.IsAvailable),
            SoldOutCount = items.Count(i => !i.IsAvailable),
            HasProfile = !string.IsNullOrWhiteSpace(DemoStore.Restaurant.Description)
                && !string.IsNullOrWhiteSpace(DemoStore.Restaurant.Address),
                HasQr = DemoStore.QrGenerated,
                RecentItems = items
                .OrderByDescending(i => i.MenuItemId)
                .Take(4)
                .Select(i => new RecentItemVm
                {
                    Name = i.Name,
                    Category = cats.FirstOrDefault(c => c.CategoryId == i.CategoryId)?.Name ?? "-",
                    Emoji = i.Emoji,
                    Price = i.Price,
                    IsAvailable = i.IsAvailable,
                    ImagePath = i.ImagePath
                }).ToList()
        };
        return View(vm);
    }

    // ---------- CATEGORIES ----------
    public IActionResult Categories()
    {
        var rows = DemoStore.Categories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryRowVm
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                IsActive = c.IsActive,
                ItemCount = DemoStore.Items.Count(i => i.CategoryId == c.CategoryId)
            }).ToList();

        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SaveCategory(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "⚠️ " + ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage;
            return RedirectToAction(nameof(Categories));
        }

        var name = model.Name.Trim();

        if (DemoStore.Categories.Any(c => c.CategoryId != model.CategoryId &&
                                          c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            TempData["Error"] = $"⚠️ A category named \"{name}\" already exists.";
            return RedirectToAction(nameof(Categories));
        }

        if (model.CategoryId == 0)
        {
            var nextOrder = DemoStore.Categories.Any() ? DemoStore.Categories.Max(c => c.DisplayOrder) + 1 : 1;
            DemoStore.Categories.Add(new Category
            {
                CategoryId = DemoStore.NextCategoryId(),
                RestaurantId = 1,
                Name = name,
                DisplayOrder = nextOrder
            });
            TempData["Success"] = $"✅ \"{name}\" added.";
        }
        else
        {
            var cat = DemoStore.Categories.FirstOrDefault(c => c.CategoryId == model.CategoryId);
            if (cat is null) return NotFound();
            cat.Name = name;
            TempData["Success"] = $"✅ Category renamed to \"{name}\".";
        }

        return RedirectToAction(nameof(Categories));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ToggleCategory(int id)
    {
        var cat = DemoStore.Categories.FirstOrDefault(c => c.CategoryId == id);
        if (cat is null) return NotFound();

        cat.IsActive = !cat.IsActive;
        TempData["Success"] = cat.IsActive
            ? $"👁️ \"{cat.Name}\" is now visible to guests."
            : $"🙈 \"{cat.Name}\" is now hidden from guests.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult MoveCategory(int id, string dir)
    {
        var ordered = DemoStore.Categories.OrderBy(c => c.DisplayOrder).ToList();
        var idx = ordered.FindIndex(c => c.CategoryId == id);
        var swap = dir == "up" ? idx - 1 : idx + 1;

        if (idx >= 0 && swap >= 0 && swap < ordered.Count)
            (ordered[idx].DisplayOrder, ordered[swap].DisplayOrder) =
                (ordered[swap].DisplayOrder, ordered[idx].DisplayOrder);

        return RedirectToAction(nameof(Categories));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DeleteCategory(int id)
    {
        var cat = DemoStore.Categories.FirstOrDefault(c => c.CategoryId == id);
        if (cat is null) return NotFound();

        var count = DemoStore.Items.Count(i => i.CategoryId == id);
        if (count > 0)
        {
            TempData["Error"] = $"⚠️ \"{cat.Name}\" still has {count} item(s). Move or delete them first.";
            return RedirectToAction(nameof(Categories));
        }

        DemoStore.Categories.Remove(cat);
        TempData["Success"] = $"🗑️ \"{cat.Name}\" deleted.";
        return RedirectToAction(nameof(Categories));
    }

    // ---------- MENU ITEMS ----------
    public IActionResult Items()
    {
        var cats = DemoStore.Categories.OrderBy(c => c.DisplayOrder).ToList();

        var items = DemoStore.Items
            .Select(i => new ItemCardVm
            {
                MenuItemId = i.MenuItemId,
                CategoryId = i.CategoryId,
                CategoryName = cats.FirstOrDefault(c => c.CategoryId == i.CategoryId)?.Name ?? "-",
                Name = i.Name,
                Description = i.Description,
                Price = i.Price,
                Emoji = i.Emoji,
                ImagePath = i.ImagePath,
                IsAvailable = i.IsAvailable
            })
            .OrderBy(i => cats.FindIndex(c => c.CategoryId == i.CategoryId))
            .ThenBy(i => i.Name)
            .ToList();

        return View(new ItemsPageViewModel { Categories = cats, Items = items });
    }

    [HttpGet]
    public IActionResult ItemForm(int? id)
    {
        if (!DemoStore.Categories.Any())
        {
            TempData["Error"] = "⚠️ Create a category first, then add dishes to it.";
            return RedirectToAction(nameof(Categories));
        }

        var vm = new ItemFormViewModel { Emoji = "🍽️" };

        if (id.HasValue)
        {
            var it = DemoStore.Items.FirstOrDefault(i => i.MenuItemId == id.Value);
            if (it is null) return NotFound();

            vm.MenuItemId = it.MenuItemId;
            vm.Name = it.Name;
            vm.CategoryId = it.CategoryId;
            vm.Price = it.Price;
            vm.Description = it.Description;
            vm.Emoji = it.Emoji;
            vm.IsAvailable = it.IsAvailable;
            vm.ExistingImagePath = it.ImagePath;
        }

        FillCategories(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ItemForm(ItemFormViewModel model)
    {
        var existing = model.MenuItemId > 0
            ? DemoStore.Items.FirstOrDefault(i => i.MenuItemId == model.MenuItemId)
            : null;
        if (model.MenuItemId > 0 && existing is null) return NotFound();

        if (model.CategoryId > 0 && !DemoStore.Categories.Any(c => c.CategoryId == model.CategoryId))
            ModelState.AddModelError(nameof(model.CategoryId), "Please choose a valid category.");

        if (model.ImageFile is { Length: > 0 })
        {
            var ext = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
            if (!AllowedImageExt.Contains(ext))
                ModelState.AddModelError(nameof(model.ImageFile), "Only JPG, PNG or WEBP images are allowed.");
            else if (model.ImageFile.Length > 2 * 1024 * 1024)
                ModelState.AddModelError(nameof(model.ImageFile), "Image must be 2 MB or smaller.");
        }

        if (!ModelState.IsValid)
        {
            model.ExistingImagePath = existing?.ImagePath;   // never trust the posted value
            FillCategories(model);
            return View(model);
        }

        string? imagePath = existing?.ImagePath;
        if (model.RemoveImage) { DeleteImageFile(imagePath); imagePath = null; }
        if (model.ImageFile is { Length: > 0 })
        {
            DeleteImageFile(imagePath);
            imagePath = await SaveImageAsync(model.ImageFile);
        }

        var emoji = string.IsNullOrWhiteSpace(model.Emoji) ? "🍽️" : model.Emoji;
        var name = model.Name.Trim();

        if (existing is null)
        {
            DemoStore.Items.Add(new MenuItem
            {
                MenuItemId = DemoStore.NextItemId(),
                RestaurantId = 1,
                CategoryId = model.CategoryId,
                Name = name,
                Description = model.Description?.Trim() ?? string.Empty,
                Price = model.Price!.Value,
                Emoji = emoji,
                ImagePath = imagePath,
                IsAvailable = model.IsAvailable
            });
            TempData["Success"] = $"✅ \"{name}\" added to your menu.";
        }
        else
        {
            existing.CategoryId = model.CategoryId;
            existing.Name = name;
            existing.Description = model.Description?.Trim() ?? string.Empty;
            existing.Price = model.Price!.Value;
            existing.Emoji = emoji;
            existing.ImagePath = imagePath;
            existing.IsAvailable = model.IsAvailable;
            TempData["Success"] = $"✅ \"{name}\" updated.";
        }

        return RedirectToAction(nameof(Items));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ToggleItem(int id)
    {
        var it = DemoStore.Items.FirstOrDefault(i => i.MenuItemId == id);
        if (it is null) return NotFound();

        it.IsAvailable = !it.IsAvailable;
        return Json(new { available = it.IsAvailable, name = it.Name });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DeleteItem(int id)
    {
        var it = DemoStore.Items.FirstOrDefault(i => i.MenuItemId == id);
        if (it is null) return NotFound();

        DeleteImageFile(it.ImagePath);
        DemoStore.Items.Remove(it);
        TempData["Success"] = $"🗑️ \"{it.Name}\" deleted.";
        return RedirectToAction(nameof(Items));
    }

    // ---------- helpers ----------
    private static readonly string[] AllowedImageExt = { ".jpg", ".jpeg", ".png", ".webp" };

    private void FillCategories(ItemFormViewModel vm) =>
        vm.Categories = DemoStore.Categories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString()))
            .ToList();

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var dir = Path.Combine(_env.WebRootPath, "uploads", "items");
        Directory.CreateDirectory(dir);

        var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var stream = System.IO.File.Create(Path.Combine(dir, fileName));
        await file.CopyToAsync(stream);

        return "/uploads/items/" + fileName;
    }

    private void DeleteImageFile(string? webPath)
    {
        if (string.IsNullOrEmpty(webPath) || !webPath.StartsWith("/uploads/items/")) return;

        var full = Path.Combine(_env.WebRootPath, "uploads", "items", Path.GetFileName(webPath));
        if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
    }

    // ---------- Placeholders (built in the next steps) ----------
    // ---------- RESTAURANT PROFILE ----------
    [HttpGet]
    public IActionResult Profile()
    {
        var r = DemoStore.Restaurant;
        return View(new ProfileFormViewModel
        {
            Name = r.Name,
            Description = r.Description,
            Address = r.Address,
            Phone = r.Phone,
            ExistingLogoPath = r.LogoPath,
            ExistingCoverPath = r.CoverPath
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileFormViewModel model)
    {
        var r = DemoStore.Restaurant;

        if (model.LogoFile is { Length: > 0 })
        {
            var ext = Path.GetExtension(model.LogoFile.FileName).ToLowerInvariant();
            if (!AllowedImageExt.Contains(ext))
                ModelState.AddModelError(nameof(model.LogoFile), "Only JPG, PNG or WEBP images are allowed.");
            else if (model.LogoFile.Length > 2 * 1024 * 1024)
                ModelState.AddModelError(nameof(model.LogoFile), "Logo must be 2 MB or smaller.");
        }

        if (model.CoverFile is { Length: > 0 })
        {
            var cext = Path.GetExtension(model.CoverFile.FileName).ToLowerInvariant();
            if (!AllowedImageExt.Contains(cext))
                ModelState.AddModelError(nameof(model.CoverFile), "Only JPG, PNG or WEBP images are allowed.");
            else if (model.CoverFile.Length > 3 * 1024 * 1024)
                ModelState.AddModelError(nameof(model.CoverFile), "Cover photo must be 3 MB or smaller.");
        }

        if (!ModelState.IsValid)
        {
            model.ExistingLogoPath = r.LogoPath;
            model.ExistingCoverPath = r.CoverPath;
            return View(model);
        }

        var logo = r.LogoPath;
        if (model.RemoveLogo) { DeleteLogoFile(logo); logo = null; }
        if (model.LogoFile is { Length: > 0 })
        {
            DeleteLogoFile(logo);
            logo = await SaveLogoAsync(model.LogoFile);
        }

        var cover = r.CoverPath;
        if (model.RemoveCover) { DeleteCoverFile(cover); cover = null; }
        if (model.CoverFile is { Length: > 0 })
        {
            DeleteCoverFile(cover);
            cover = await SaveCoverAsync(model.CoverFile);
        }

        r.Name = model.Name.Trim();
        r.Description = model.Description?.Trim() ?? string.Empty;
        r.Address = model.Address?.Trim() ?? string.Empty;
        r.Phone = model.Phone?.Trim() ?? string.Empty;
        r.LogoPath = logo;
        r.CoverPath = cover;

        TempData["Success"] = "✅ Restaurant profile saved.";
        return RedirectToAction(nameof(Profile));
    }

    // ---------- QR CODE ----------
    public IActionResult Qr()
    {
        DemoStore.QrGenerated = true;
        ViewData["MenuUrl"] = $"{Request.Scheme}://{Request.Host}/m/{DemoStore.Restaurant.Slug}";
        return View(DemoStore.Restaurant);
    }

    private async Task<string> SaveLogoAsync(IFormFile file)
    {
        var dir = Path.Combine(_env.WebRootPath, "uploads", "logos");
        Directory.CreateDirectory(dir);
        var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var stream = System.IO.File.Create(Path.Combine(dir, fileName));
        await file.CopyToAsync(stream);
        return "/uploads/logos/" + fileName;
    }

    private void DeleteLogoFile(string? webPath)
    {
        if (string.IsNullOrEmpty(webPath) || !webPath.StartsWith("/uploads/logos/")) return;
        var full = Path.Combine(_env.WebRootPath, "uploads", "logos", Path.GetFileName(webPath));
        if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
    }

    private async Task<string> SaveCoverAsync(IFormFile file)
    {
        var dir = Path.Combine(_env.WebRootPath, "uploads", "covers");
        Directory.CreateDirectory(dir);
        var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var stream = System.IO.File.Create(Path.Combine(dir, fileName));
        await file.CopyToAsync(stream);
        return "/uploads/covers/" + fileName;
    }

    private void DeleteCoverFile(string? webPath)
    {
        if (string.IsNullOrEmpty(webPath) || !webPath.StartsWith("/uploads/covers/")) return;
        var full = Path.Combine(_env.WebRootPath, "uploads", "covers", Path.GetFileName(webPath));
        if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
    }
}