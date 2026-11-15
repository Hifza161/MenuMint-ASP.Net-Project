using MenuMint.Interfaces;
using MenuMint.Models;
using MenuMint.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MenuMint.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IRestaurantSetupService _restaurantSetup;
    private readonly ILogger<AccountController> _logger;

    public AccountController(UserManager<ApplicationUser> userManager,
                             SignInManager<ApplicationUser> signInManager,
                             IRestaurantSetupService restaurantSetup,
                             ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _restaurantSetup = restaurantSetup;
        _logger = logger;
    }

    // ---------- LOGIN ----------
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect(User.IsInRole("Admin") ? "/Admin/Dashboard" : "/Owner/Dashboard");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This account has been deactivated. Please contact the administrator.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);

            return Redirect(await _userManager.IsInRoleAsync(user, "Admin")
                ? "/Admin/Dashboard"
                : "/Owner/Dashboard");
        }

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "Too many failed attempts. Try again in a few minutes."
            : "Invalid email or password.");
        return View(model);
    }

    // ---------- REGISTER ----------
    [HttpGet, AllowAnonymous]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        if (await _userManager.FindByEmailAsync(model.Email) is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.OwnerName.Trim(),
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        // The user exists now. The role and the restaurant must succeed too, or we undo the user.
        try
        {
            var roleResult = await _userManager.AddToRoleAsync(user, "Owner");
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(
                    "Could not assign the Owner role: " + string.Join("; ", roleResult.Errors.Select(e => e.Description)));

            await _restaurantSetup.CreateForOwnerAsync(user.Id, model.RestaurantName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed after the user {Email} was created. Removing the user again.", model.Email);

            var undo = await _userManager.DeleteAsync(user);
            if (!undo.Succeeded)
                _logger.LogError("Could not remove the half-created user {Email}.", model.Email);

            ModelState.AddModelError(string.Empty, "We couldn't finish creating your account. Please try again.");
            return View(model);
        }

        TempData["Success"] = "Account created! Please log in.";
        return RedirectToAction(nameof(Login));
    }

    // ---------- LOGOUT ----------
    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // ---------- ACCESS DENIED ----------
    [HttpGet, AllowAnonymous]
    public IActionResult AccessDenied() => View();
}