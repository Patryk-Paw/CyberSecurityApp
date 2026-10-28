
using CyberSecurityApp.Data;
using Microsoft.EntityFrameworkCore;

namespace CyberSecurityApp.Services;

public class PasswordPolicyService
{
    private readonly ApplicationDbContext _context;

    private static readonly string[] Sequences =
    [
        "qwertyuiop",
        "asdfghjkl",
        "zxcvbnm",
        "1234567890",
        "abcdefghijklmnopqrstuvwxyz"
    ];

    public PasswordPolicyService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string?> ValidateAsync(
        string password,
        CancellationToken cancellationToken = default)
    {
        var settings = await _context.SecuritySettings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                setting => setting.Id == 1,
                cancellationToken);

        var minimumLength = settings?.MinimumPasswordLength ?? 15;

        if (password.Length < minimumLength)
        {
            return $"Hasło musi mieć co najmniej {minimumLength} znaków.";
        }

        if (settings?.EnforceSequencePolicy == true &&
            ContainsSequence(password))
        {
            return "Hasło nie może zawierać sekwencji klawiaturowych ani rosnących ciągów.";
        }

        return null;
    }

    private static bool ContainsSequence(string password)
    {
        var normalizedPassword = password.ToLowerInvariant();

        foreach (var sequence in Sequences)
        {
            for (var length = 4; length <= sequence.Length; length++)
            {
                for (var start = 0; start <= sequence.Length - length; start++)
                {
                    var part = sequence.Substring(start, length);

                    if (normalizedPassword.Contains(part))
                    {
                        return true;
                    }

                    var reversed = new string(part.Reverse().ToArray());

                    if (normalizedPassword.Contains(reversed))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
    