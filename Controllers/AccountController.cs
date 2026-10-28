using CyberSecurityApp.Models;
using CyberSecurityApp.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CyberSecurityApp.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;

    public AccountController(
        SignInManager<User> signInManager,
        UserManager<User> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByNameAsync(model.UserName);

        if (user == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Login lub hasło niepoprawne");

            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(
                string.Empty,
                "Konto zablokowane. Skontaktuj się z administratorem.");

            return View(model);
        }

        if (user.MustChangePassword &&
            user.OneTimePasswordExpiresAt.HasValue &&
            user.OneTimePasswordExpiresAt.Value <= DateTime.UtcNow)
        {
            ModelState.AddModelError(
                string.Empty,
                "Hasło jednorazowe wygasło. Skontaktuj się z administratorem.");

            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            if (user.MustChangePassword)
            {
                return RedirectToAction("ChangePassword", "Password");
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Konto jest zablokowane. Spróbuj ponownie za 15 minut.");

            return View(model);
        }

        ModelState.AddModelError(
            string.Empty,
            "Login lub hasło niepoprawne");

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
