using MenuMint.Data;
using MenuMint.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MenuMint.Controllers;

[AllowAnonymous]
public class MenuController : Controller
{
    [HttpGet("m/{slug}")]
    public IActionResult Show(string slug)
    {
        // TODO (ADO.NET step): SELECT restaurant WHERE Slug = @slug
        var r = DemoStore.Restaurant;

        if (!string.Equals(r.Slug, slug, StringComparison.OrdinalIgnoreCase))
        {
            Response.StatusCode = 404;
            ViewData["Heading"] = "We couldn't find that menu";
            ViewData["Text"] = "The link may be mistyped, or the restaurant may have changed it.";
            return View("Unavailable");
        }

        if (!r.IsActive)
        {
            Response.StatusCode = 404;
            ViewData["Heading"] = "This menu is unavailable";
            ViewData["Text"] = "This restaurant's menu is not available right now.";
            return View("Unavailable");
        }

        var sections = DemoStore.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new PublicSectionVm
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Items = DemoStore.Items
                    .Where(i => i.CategoryId == c.CategoryId)
                    .OrderByDescending(i => i.IsAvailable)
                    .ThenBy(i => i.Name)
                    .Select(i => new PublicItemVm
                    {
                        Name = i.Name,
                        Description = i.Description,
                        Price = i.Price,
                        Emoji = i.Emoji,
                        ImagePath = i.ImagePath,
                        IsAvailable = i.IsAvailable
                    }).ToList()
            })
            .Where(s => s.Items.Any())
            .ToList();

        return View(new PublicMenuViewModel
        {
            Name = r.Name,
            Description = r.Description,
            Address = r.Address,
            Phone = r.Phone,
            LogoPath = r.LogoPath,
            Sections = sections,
            CoverPath = r.CoverPath
        });
    }
}