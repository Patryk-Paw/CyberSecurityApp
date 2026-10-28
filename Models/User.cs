using Microsoft.AspNetCore.Identity;
namespace CyberSecurityApp.Models;

public class User : IdentityUser
{
    public bool MustChangePassword { get; set; } = true;

    public DateTime? PasswordChangedAt { get; set; }

    public DateTime? OneTimePasswordExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}