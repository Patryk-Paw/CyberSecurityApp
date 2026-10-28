using CyberSecurityApp.Data;
using CyberSecurityApp.Models;
using CyberSecurityApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberSecurityApp.Controllers;

[Authorize(Roles = "Administrator")]
public class SecuritySettingsController : Controller
{
    private readonly ApplicationDbContext _context;

    public SecuritySettingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var settings = await _context.SecuritySettings
            .FirstOrDefaultAsync(item => item.Id == 1);

        if (settings == null)
        {
            settings = new SecuritySettings
            {
                Id = 1,
                MinimumPasswordLength = 15,
                EnforceSequencePolicy = false,
                PasswordExpirationDays = 90,
                SessionIdleTimeoutMinutes = 20,
                OneTimePasswordValidityHours = 24
            };

            _context.SecuritySettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        var model = new SecuritySettingsViewModel
        {
            MinimumPasswordLength = settings.MinimumPasswordLength,
            EnforceSequencePolicy = settings.EnforceSequencePolicy,
            PasswordExpirationDays = settings.PasswordExpirationDays,
            SessionIdleTimeoutMinutes = settings.SessionIdleTimeoutMinutes,
            OneTimePasswordValidityHours = settings.OneTimePasswordValidityHours
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SecuritySettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var settings = await _context.SecuritySettings
            .FirstOrDefaultAsync(item => item.Id == 1);

        if (settings == null)
        {
            settings = new SecuritySettings { Id = 1 };
            _context.SecuritySettings.Add(settings);
        }

        settings.MinimumPasswordLength = model.MinimumPasswordLength;
        settings.EnforceSequencePolicy = model.EnforceSequencePolicy;
        settings.PasswordExpirationDays = model.PasswordExpirationDays;
        settings.SessionIdleTimeoutMinutes = model.SessionIdleTimeoutMinutes;
        settings.OneTimePasswordValidityHours = model.OneTimePasswordValidityHours;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Ustawienia bezpieczeństwa zostały zapisane.";

        return RedirectToAction(nameof(Index));
    }
}
