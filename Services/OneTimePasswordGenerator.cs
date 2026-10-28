using System.Security.Cryptography;
using CyberSecurityApp.Data;
using Microsoft.EntityFrameworkCore;

namespace CyberSecurityApp.Services;

public class OneTimePasswordGenerator
{
    private readonly ApplicationDbContext _context;

    public OneTimePasswordGenerator(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateAsync(
        CancellationToken cancellationToken = default)
    {
        var words = await _context.PasswordDictionaryWords
            .AsNoTracking()
            .Select(item => item.Word)
            .ToListAsync(cancellationToken);

        if (words.Count < 4)
        {
            throw new InvalidOperationException(
                "Słownik musi zawierać co najmniej 4 słowa.");
        }

        var selectedWords = new List<string>(4);

        while (selectedWords.Count < 4)
        {
            var index = RandomNumberGenerator.GetInt32(words.Count);
            var word = words[index];

            if (!selectedWords.Contains(word))
            {
                selectedWords.Add(word);
            }

            words.RemoveAt(index);
        }

        return string.Join("-", selectedWords);
    }
}