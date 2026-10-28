using CyberSecurityApp.Data;
using CyberSecurityApp.Models;
using CyberSecurityApp.Services;
using CyberSecurityApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberSecurityApp.Controllers;

[Authorize]
public class PasswordController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly PasswordPolicyService _passwordPolicyService;

    public PasswordController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ApplicationDbContext context,
        PasswordPolicyService passwordPolicyService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _passwordPolicyService = passwordPolicyService;
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        var currentPasswordValid = await _userManager.CheckPasswordAsync(
            user,
            model.CurrentPassword);

        if (!currentPasswordValid)
        {
            ModelState.AddModelError(
                nameof(model.CurrentPassword),
                "Obecne hasło jest niepoprawne.");

            return View(model);
        }

        if (model.CurrentPassword == model.NewPassword)
        {
            ModelState.AddModelError(
                nameof(model.NewPassword),
                "Nowe hasło musi różnić się od obecnego.");

            return View(model);
        }

        var policyError = await _passwordPolicyService.ValidateAsync(
            model.NewPassword);

        if (policyError != null)
        {
            ModelState.AddModelError(nameof(model.NewPassword), policyError);
            return View(model);
        }

        var passwordHistory = await _context.PasswordHistories
            .Where(history => history.UserId == user.Id)
            .ToListAsync();

        foreach (var history in passwordHistory)
        {
            var verificationUser = new User
            {
                PasswordHash = history.PasswordHash
            };

            var verificationResult = _userManager.PasswordHasher.VerifyHashedPassword(
                verificationUser,
                history.PasswordHash,
                model.NewPassword);

            if (verificationResult != PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    nameof(model.NewPassword),
                    "Nie możesz użyć hasła, którego używałeś wcześniej.");

                return View(model);
            }
        }

        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            _context.PasswordHistories.Add(new PasswordHistory
            {
                UserId = user.Id,
                PasswordHash = user.PasswordHash,
                CreatedAt = DateTime.UtcNow
            });
        }

        var changeResult = await _userManager.ChangePasswordAsync(
            user,
            model.CurrentPassword,
            model.NewPassword);

        if (!changeResult.Succeeded)
        {
            foreach (var error in changeResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        user.MustChangePassword = false;
        user.PasswordChangedAt = DateTime.UtcNow;
        user.OneTimePasswordExpiresAt = null;

        await _userManager.UpdateAsync(user);
        await _context.SaveChangesAsync();
        await _signInManager.RefreshSignInAsync(user);

        TempData["SuccessMessage"] = "Hasło zostało zmienione.";

        return RedirectToAction("Index", "Home");
    }
}
