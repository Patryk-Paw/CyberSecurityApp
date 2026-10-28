using System.Security.Cryptography;
using CyberSecurityApp.Data;
using CyberSecurityApp.Models;
using CyberSecurityApp.Services;
using CyberSecurityApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberSecurityApp.Controllers;

[Authorize(Roles = "Administrator")]
public class AdminController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly OneTimePasswordGenerator _passwordGenerator;
    private readonly PasswordPolicyService _passwordPolicy;

    public AdminController(
        UserManager<User> userManager,
        ApplicationDbContext context,
        OneTimePasswordGenerator passwordGenerator,
        PasswordPolicyService passwordPolicy)
    {
        _userManager = userManager;
        _context = context;
        _passwordGenerator = passwordGenerator;
        _passwordPolicy = passwordPolicy;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderBy(user => user.UserName)
            .ToListAsync();

        var model = new List<AdminUserViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            model.Add(new AdminUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "Brak roli",
                IsActive = user.IsActive,
                MustChangePassword = user.MustChangePassword,
                CreatedAt = user.CreatedAt,
                PasswordChangedAt = user.PasswordChangedAt
            });
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult CreateUser()
    {
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model)
    {
        var allowedRoles = new[] { "Redaktor", "Czytelnik" };

        if (!allowedRoles.Contains(model.Role))
        {
            ModelState.AddModelError(
                nameof(model.Role),
                "Wybrano nieprawidłową rolę.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var settings = await _context.SecuritySettings
            .AsNoTracking()
            .FirstOrDefaultAsync(setting => setting.Id == 1);

        var validityHours = settings?.OneTimePasswordValidityHours ?? 24;

        string temporaryPassword;

        try
        {
            temporaryPassword = string.Empty;

            for (var attempt = 0; attempt < 100; attempt++)
            {
                var candidate = await _passwordGenerator.GenerateAsync();
                var policyError = await _passwordPolicy.ValidateAsync(candidate);

                if (policyError == null)
                {
                    temporaryPassword = candidate;
                    break;
                }
            }

            if (string.IsNullOrEmpty(temporaryPassword))
            {
                TempData["ErrorMessage"] =
                    "Nie udało się wygenerować hasła zgodnego z polityką bezpieczeństwa. " +
                    "Dodaj więcej słów do słownika lub sprawdź ustawienia haseł.";

                return RedirectToAction(nameof(Index));
            }
        }
        catch (InvalidOperationException)
        {
            TempData["ErrorMessage"] =
                "Słownik musi zawierać co najmniej 4 słowa.";

            return RedirectToAction(nameof(Index));
        }

        string userName;

        do
        {
            userName = "user-" +
                Convert.ToHexString(RandomNumberGenerator.GetBytes(4))
                    .ToLowerInvariant();
        }
        while (await _userManager.FindByNameAsync(userName) != null);

        var user = new User
        {
            UserName = userName,
            IsActive = true,
            MustChangePassword = true,
            OneTimePasswordExpiresAt = DateTime.UtcNow.AddHours(validityHours),
            CreatedAt = DateTime.UtcNow,
            PasswordChangedAt = null
        };

        var createResult = await _userManager.CreateAsync(
            user,
            temporaryPassword);

        if (!createResult.Succeeded)
        {
            TempData["ErrorMessage"] =
                "Nie udało się utworzyć konta: " +
                string.Join(" ", createResult.Errors.Select(error => error.Description));

            return RedirectToAction(nameof(Index));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, model.Role);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            TempData["ErrorMessage"] =
                "Nie udało się przypisać roli. Konto nie zostało utworzone.";

            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Utworzono konto użytkownika.";
        TempData["GeneratedUserName"] = userName;
        TempData["GeneratedPassword"] = temporaryPassword;
        TempData["PasswordExpiresAt"] =
            user.OneTimePasswordExpiresAt?.ToLocalTime()
                .ToString("dd.MM.yyyy HH:mm");

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);

        if (currentUser == null || currentUser.Id == user.Id)
        {
            TempData["ErrorMessage"] =
                "Nie możesz zmienić statusu własnego konta.";

            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] =
                "Nie udało się zmienić statusu konta.";

            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = user.IsActive
            ? $"Konto {user.UserName} zostało odblokowane."
            : $"Konto {user.UserName} zostało zablokowane.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains("Administrator") ||
            !roles.Any(role => role == "Redaktor" || role == "Czytelnik"))
        {
            TempData["ErrorMessage"] = "Nie mozna edytowac tego konta.";
            return RedirectToAction(nameof(Index));
        }

        return View(new EditUserViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Role = roles.First()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (model.Role != "Redaktor" && model.Role != "Czytelnik")
        {
            ModelState.AddModelError(nameof(model.Role), "Nieprawidlowa rola.");
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);

        if (user == null)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);

        if (currentUser?.Id == user.Id)
        {
            TempData["ErrorMessage"] = "Nie mozna edytowac wlasnego konta w tym formularzu.";
            return RedirectToAction(nameof(Index));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);

        if (currentRoles.Contains("Administrator"))
        {
            TempData["ErrorMessage"] = "Nie mozna edytowac konta administratora.";
            return RedirectToAction(nameof(Index));
        }

        var existingUser = await _userManager.FindByNameAsync(model.UserName.Trim());

        if (existingUser != null && existingUser.Id != user.Id)
        {
            ModelState.AddModelError(nameof(model.UserName), "Ten login jest juz zajety.");
            return View(model);
        }

        user.UserName = model.UserName.Trim();

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        var rolesToRemove = currentRoles
            .Where(role => role == "Redaktor" || role == "Czytelnik")
            .ToArray();

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            if (!removeResult.Succeeded)
            {
                TempData["ErrorMessage"] = "Nie udalo sie zmienic roli uzytkownika.";
                return RedirectToAction(nameof(Index));
            }
        }

        var addResult = await _userManager.AddToRoleAsync(user, model.Role);

        if (!addResult.Succeeded)
        {
            TempData["ErrorMessage"] = "Nie udalo sie przypisac nowej roli.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Dane uzytkownika zostaly zaktualizowane.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        if (roles.Contains("Administrator"))
        {
            TempData["ErrorMessage"] = "Nie mozna usunac konta administratora.";
            return RedirectToAction(nameof(Index));
        }

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUserConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user);

        if (currentUser?.Id == user.Id || roles.Contains("Administrator"))
        {
            TempData["ErrorMessage"] = "Nie mozna usunac tego konta.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = "Nie udalo sie usunac uzytkownika.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Uzytkownik zostal usuniety.";

        return RedirectToAction(nameof(Index));
    }
}