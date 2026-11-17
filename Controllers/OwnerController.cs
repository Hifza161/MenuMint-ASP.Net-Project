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
    private readonly ICurrentRestaurantService _current;
    private readonly IRestaurantSetupService _setup;
    private readonly IRestaurantRepository _restaurants;
    private readonly ICategoryRepository _categories;
    private readonly IMenuItemRepository _items;
    private readonly IImageStorageService _images;

    public OwnerController(UserManager<ApplicationUser> userManager,
        ICurrentRestaurantService current, IRestaurantSetupService setup,
        IRestaurantRepository restaurants, ICategoryRepository categories,
        IMenuItemRepository items, IImageStorageService images)
    {
        _userManager = userManager;
        _current = current;
        _setup = setup;
        _restaurants = restaurants;
        _categories = categories;
        _items = items;
        _images = images;
    }

    private async Task<int> RidAsync() => (await _current.GetIdAsync())!.Value;

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var restaurant = await _current.GetAsync();
        var action = context.RouteData.Values["action"]?.ToString();
        var isSetup = string.Equals(action, nameof(Setup), StringComparison.OrdinalIgnoreCase);

        if (restaurant is null && !isSetup)
        {
            context.Result = RedirectToAction(nameof(Setup));
            return;
        }

        var user = await _userManager.GetUserAsync(User);
        ViewData["OwnerName"] = user?.FullName ?? "Owner";

        if (restaurant is not null)
        {
            ViewData["RestaurantName"] = restaurant.Name;
            ViewData["Slug"] = restaurant.Slug;
        }

        await next();
    }

    // ---------- SETUP ----------
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
    public async Task<IActionResult> Dashboard()
    {
        var r = (await _current.GetAsync())!;
        var categories = await _categories.GetSummariesAsync(r.RestaurantId);
        var counts = await _items.GetCountsAsync(r.RestaurantId);
        var recent = await _items.GetRecentAsync(r.RestaurantId, 4);

        return View(new DashboardViewModel
        {
            OwnerName = (string)ViewData["OwnerName"]!,
            RestaurantName = r.Name,
            Slug = r.Slug,
            CategoryCount = categories.Count,
            ItemCount = counts.Total,
            AvailableCount = counts.Available,
            SoldOutCount = counts.SoldOut,
            HasProfile = !string.IsNullOrWhiteSpace(r.Description) && !string.IsNullOrWhiteSpace(r.Address),
            HasQr = r.QrGeneratedAt is not null,
            RecentItems = recent.Select(i => new RecentItemVm
            {
                Name = i.Name,
                Category = i.CategoryName,
                Emoji = string.IsNullOrWhiteSpace(i.Emoji) ? "🍽️" : i.Emoji,
                Price = i.Price,
                IsAvailable = i.IsAvailable,
                ImagePath = i.ImagePath
            }).ToList()
        });
    }

    // ---------- PROFILE ----------
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var r = (await _current.GetAsync())!;
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
        var r = (await _current.GetAsync())!;

        if (model.LogoFile is { Length: > 0 })
        {
            var error = await _images.ValidateAsync(model.LogoFile, ImageFolder.Logos);
            if (error is not null) ModelState.AddModelError(nameof(model.LogoFile), error);
        }
        if (model.CoverFile is { Length: > 0 })
        {
            var error = await _images.ValidateAsync(model.CoverFile, ImageFolder.Covers);
            if (error is not null) ModelState.AddModelError(nameof(model.CoverFile), error);
        }

        if (!ModelState.IsValid)
        {
            model.ExistingLogoPath = r.LogoPath;
            model.ExistingCoverPath = r.CoverPath;
            return View(model);
        }

        var logo = r.LogoPath;
        var cover = r.CoverPath;
        string? newLogo = null, newCover = null;
        var oldFiles = new List<string?>();

        if (model.LogoFile is { Length: > 0 })
        {
            newLogo = await _images.SaveAsync(model.LogoFile, ImageFolder.Logos);
            oldFiles.Add(r.LogoPath); logo = newLogo;
        }
        else if (model.RemoveLogo) { oldFiles.Add(r.LogoPath); logo = null; }

        if (model.CoverFile is { Length: > 0 })
        {
            newCover = await _images.SaveAsync(model.CoverFile, ImageFolder.Covers);
            oldFiles.Add(r.CoverPath); cover = newCover;
        }
        else if (model.RemoveCover) { oldFiles.Add(r.CoverPath); cover = null; }

        try
        {
            await _restaurants.UpdateProfileAsync(new Restaurant
            {
                RestaurantId = r.RestaurantId,
                Name = model.Name.Trim(),
                Description = model.Description?.Trim() ?? string.Empty,
                Address = model.Address?.Trim() ?? string.Empty,
                Phone = model.Phone?.Trim() ?? string.Empty,
                LogoPath = logo,
                CoverPath = cover
            });
        }
        catch
        {
            _images.Delete(newLogo);
            _images.Delete(newCover);
            throw;
        }

        foreach (var path in oldFiles) _images.Delete(path);
        _current.Invalidate();

        TempData["Success"] = "✅ Restaurant profile saved.";
        return RedirectToAction(nameof(Profile));
    }

    // ---------- QR ----------
    public async Task<IActionResult> Qr()
    {
        var r = (await _current.GetAsync())!;
        await _restaurants.MarkQrGeneratedAsync(r.RestaurantId);
        ViewData["MenuUrl"] = $"{Request.Scheme}://{Request.Host}/m/{r.Slug}";
        return View(r);
    }

    // =====================================================================
    //  CATEGORIES (Dapper repository)
    // =====================================================================
    public async Task<IActionResult> Categories()
    {
        var rid = await RidAsync();
        var rows = (await _categories.GetSummariesAsync(rid))
            .Select(c => new CategoryRowVm { CategoryId = c.CategoryId, Name = c.Name, IsActive = c.IsActive, ItemCount = c.ItemCount })
            .ToList();
        return View(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "⚠️ " + ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage;
            return RedirectToAction(nameof(Categories));
        }

        var rid = await RidAsync();
        var name = model.Name.Trim();

        if (await _categories.NameExistsAsync(rid, name, model.CategoryId == 0 ? null : model.CategoryId))
        {
            TempData["Error"] = $"⚠️ A category named \"{name}\" already exists.";
            return RedirectToAction(nameof(Categories));
        }

        if (model.CategoryId == 0)
        {
            await _categories.CreateAsync(rid, name);
            TempData["Success"] = $"✅ \"{name}\" added.";
        }
        else
        {
            if (!await _categories.RenameAsync(model.CategoryId, rid, name)) return NotFound();
            TempData["Success"] = $"✅ Category renamed to \"{name}\".";
        }
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCategory(int id)
    {
        var rid = await RidAsync();
        var cat = await _categories.GetAsync(id, rid);
        if (cat is null) return NotFound();

        await _categories.SetActiveAsync(id, rid, !cat.IsActive);
        TempData["Success"] = !cat.IsActive
            ? $"👁️ \"{cat.Name}\" is now visible to guests."
            : $"🙈 \"{cat.Name}\" is now hidden from guests.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveCategory(int id, string dir)
    {
        await _categories.MoveAsync(id, await RidAsync(), up: dir == "up");
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var rid = await RidAsync();
        var cat = await _categories.GetAsync(id, rid);
        if (cat is null) return NotFound();

        switch (await _categories.DeleteAsync(id, rid))
        {
            case DeleteCategoryResult.Deleted:
                TempData["Success"] = $"🗑️ \"{cat.Name}\" deleted."; break;
            case DeleteCategoryResult.HasItems:
                TempData["Error"] = $"⚠️ \"{cat.Name}\" still has items. Move or delete them first."; break;
            default:
                return NotFound();
        }
        return RedirectToAction(nameof(Categories));
    }

    // One click fills an empty restaurant with a sample menu (4 categories, 9 dishes with photos)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LoadSample()
    {
        var rid = await RidAsync();
        if ((await _categories.GetSummariesAsync(rid)).Any())
        {
            TempData["Error"] = "⚠️ A sample menu can only be loaded into an empty menu.";
            return RedirectToAction(nameof(Categories));
        }

        var starters = await _categories.CreateAsync(rid, "Starters");
        var mains = await _categories.CreateAsync(rid, "Main Course");
        var desserts = await _categories.CreateAsync(rid, "Desserts");
        var drinks = await _categories.CreateAsync(rid, "Beverages");

        Task Add(int cat, string name, string desc, decimal price, string emoji, string file, bool available = true) =>
            _items.CreateAsync(new MenuItem
            {
                RestaurantId = rid,
                CategoryId = cat,
                Name = name,
                Description = desc,
                Price = price,
                Emoji = emoji,
                ImagePath = "/images/dishes/" + file,
                IsAvailable = available
            });

        await Add(starters, "Bruschetta", "Tomato, basil, olive oil", 650, "🥖", "bruschetta.jpg");
        await Add(starters, "Crispy Wings", "Smoky BBQ glaze", 850, "🍗", "crispy-wings.jpg");
        await Add(mains, "Margherita Pizza", "Fresh basil, mozzarella", 1450, "🍕", "margherita-pizza.jpg");
        await Add(mains, "Smash Burger", "Double patty, house sauce", 1250, "🍔", "smash-burger.jpg");
        await Add(mains, "Alfredo Pasta", "Creamy parmesan sauce", 1350, "🍝", "alfredo-pasta.jpg", available: false);
        await Add(desserts, "Lava Cake", "Warm chocolate center", 750, "🍰", "lava-cake.jpg");
        await Add(desserts, "Mango Kulfi", "Pistachio topping", 450, "🍨", "mango-kulfi.jpg");
        await Add(drinks, "Mint Margarita", "Fresh mint, lime, soda", 550, "🍹", "mint-margarita.jpg");
        await Add(drinks, "Cold Coffee", "Iced, creamy, bold", 600, "☕", "cold-coffee.jpg");

        TempData["Success"] = "✨ Sample menu loaded.";
        return RedirectToAction(nameof(Categories));
    }

    // =====================================================================
    //  MENU ITEMS (Dapper repository)
    // =====================================================================
    public async Task<IActionResult> Items()
    {
        var rid = await RidAsync();
        var cats = await _categories.GetAllAsync(rid);
        var rows = await _items.GetByRestaurantAsync(rid);

        return View(new ItemsPageViewModel
        {
            Categories = cats,
            Items = rows.Select(i => new ItemCardVm
            {
                MenuItemId = i.MenuItemId,
                CategoryId = i.CategoryId,
                CategoryName = i.CategoryName,
                Name = i.Name,
                Description = i.Description,
                Price = i.Price,
                Emoji = string.IsNullOrWhiteSpace(i.Emoji) ? "🍽️" : i.Emoji,
                ImagePath = i.ImagePath,
                IsAvailable = i.IsAvailable
            }).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> ItemForm(int? id)
    {
        var rid = await RidAsync();

        if (!(await _categories.GetAllAsync(rid)).Any())
        {
            TempData["Error"] = "⚠️ Create a category first, then add dishes to it.";
            return RedirectToAction(nameof(Categories));
        }

        var vm = new ItemFormViewModel { Emoji = "🍽️" };

        if (id.HasValue)
        {
            var it = await _items.GetAsync(id.Value, rid);
            if (it is null) return NotFound();

            vm.MenuItemId = it.MenuItemId;
            vm.Name = it.Name;
            vm.CategoryId = it.CategoryId;
            vm.Price = it.Price;
            vm.Description = it.Description;
            vm.Emoji = string.IsNullOrWhiteSpace(it.Emoji) ? "🍽️" : it.Emoji;
            vm.IsAvailable = it.IsAvailable;
            vm.ExistingImagePath = it.ImagePath;
        }

        await FillCategoriesAsync(vm, rid);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ItemForm(ItemFormViewModel model)
    {
        var rid = await RidAsync();

        MenuItem? existing = null;
        if (model.MenuItemId > 0)
        {
            existing = await _items.GetAsync(model.MenuItemId, rid);
            if (existing is null) return NotFound();
        }

        if (model.ImageFile is { Length: > 0 })
        {
            var error = await _images.ValidateAsync(model.ImageFile, ImageFolder.Items);
            if (error is not null) ModelState.AddModelError(nameof(model.ImageFile), error);
        }

        if (!ModelState.IsValid)
        {
            model.ExistingImagePath = existing?.ImagePath;
            await FillCategoriesAsync(model, rid);
            return View(model);
        }

        var imagePath = existing?.ImagePath;
        string? newImage = null, oldImage = null;

        if (model.ImageFile is { Length: > 0 })
        {
            newImage = await _images.SaveAsync(model.ImageFile, ImageFolder.Items);
            oldImage = existing?.ImagePath;
            imagePath = newImage;
        }
        else if (model.RemoveImage)
        {
            oldImage = existing?.ImagePath;
            imagePath = null;
        }

        var item = new MenuItem
        {
            MenuItemId = existing?.MenuItemId ?? 0,
            RestaurantId = rid,                        // from the logged-in owner, never from the form
            CategoryId = model.CategoryId,
            Name = model.Name.Trim(),
            Description = model.Description?.Trim() ?? string.Empty,
            Price = model.Price!.Value,
            Emoji = string.IsNullOrWhiteSpace(model.Emoji) ? "🍽️" : model.Emoji,
            ImagePath = imagePath,
            IsAvailable = model.IsAvailable
        };

        var saved = existing is null ? await _items.CreateAsync(item) > 0 : await _items.UpdateAsync(item);
        if (!saved)
        {
            _images.Delete(newImage);                  // no orphan file if the category was not this owner's
            ModelState.AddModelError(nameof(model.CategoryId), "Please choose a valid category.");
            model.ExistingImagePath = existing?.ImagePath;
            await FillCategoriesAsync(model, rid);
            return View(model);
        }

        _images.Delete(oldImage);
        TempData["Success"] = existing is null ? $"✅ \"{item.Name}\" added to your menu." : $"✅ \"{item.Name}\" updated.";
        return RedirectToAction(nameof(Items));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleItem(int id)
    {
        var result = await _items.ToggleAvailabilityAsync(id, await RidAsync());
        if (result is null) return NotFound();
        return Json(new { available = result.IsAvailable, name = result.Name });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var deleted = await _items.DeleteAsync(id, await RidAsync());
        if (deleted is null) return NotFound();

        _images.Delete(deleted.ImagePath);
        TempData["Success"] = $"🗑️ \"{deleted.Name}\" deleted.";
        return RedirectToAction(nameof(Items));
    }

    private async Task FillCategoriesAsync(ItemFormViewModel vm, int restaurantId) =>
        vm.Categories = (await _categories.GetAllAsync(restaurantId))
            .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString()))
            .ToList();
}