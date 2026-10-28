using CyberSecurityApp.Data;
using CyberSecurityApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CyberSecurityApp.Controllers;

[Authorize(Roles = "Administrator")]
public class PasswordDictionaryController : Controller
{
    private readonly ApplicationDbContext _context;

    public PasswordDictionaryController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var words = await _context.PasswordDictionaryWords
            .AsNoTracking()
            .OrderBy(word => word.Word)
            .ToListAsync();

        return View(words);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string word)
    {
        word = word?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(word))
        {
            TempData["ErrorMessage"] = "Wpisz słowo.";
            return RedirectToAction(nameof(Index));
        }

        if (word.Length > 50)
        {
            TempData["ErrorMessage"] = "Słowo nie może mieć więcej niż 50 znaków.";
            return RedirectToAction(nameof(Index));
        }

        var normalizedWord = word.ToLowerInvariant();

        var exists = await _context.PasswordDictionaryWords
            .AnyAsync(item => item.Word.ToLower() == normalizedWord);

        if (exists)
        {
            TempData["ErrorMessage"] = "Takie słowo już znajduje się w słowniku.";
            return RedirectToAction(nameof(Index));
        }

        _context.PasswordDictionaryWords.Add(new PasswordDictionaryWord
        {
            Word = normalizedWord,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Słowo zostało dodane.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var word = await _context.PasswordDictionaryWords
            .FirstOrDefaultAsync(item => item.Id == id);

        if (word == null)
        {
            TempData["ErrorMessage"] = "Nie znaleziono wskazanego słowa.";
            return RedirectToAction(nameof(Index));
        }

        _context.PasswordDictionaryWords.Remove(word);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Słowo zostało usunięte.";
        return RedirectToAction(nameof(Index));
    }
}