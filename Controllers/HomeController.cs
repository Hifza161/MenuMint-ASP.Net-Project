using Microsoft.AspNetCore.Mvc;

namespace MenuMint.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    // Shown for 404 / 403 / etc. (wired up in Program.cs)
    public IActionResult Status(int code = 404) => View("StatusPage", code);

    // Shown for unhandled exceptions in non-Development environments
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View("StatusPage", 500);
}