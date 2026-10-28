using CyberSecurityApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CyberSecurityApp.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();

    public DbSet<SecuritySettings> SecuritySettings => Set<SecuritySettings>();

    public DbSet<PasswordDictionaryWord> PasswordDictionaryWords => Set<PasswordDictionaryWord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<PasswordHistory>()
            .HasOne(history => history.User)
            .WithMany()
            .HasForeignKey(history => history.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PasswordHistory>()
            .Property(history => history.PasswordHash)
            .IsRequired();

        builder.Entity<PasswordDictionaryWord>()
            .Property(word => word.Word)
            .IsRequired();

        builder.Entity<PasswordDictionaryWord>()
            .HasIndex(word => word.Word)
            .IsUnique();

        builder.Entity<SecuritySettings>()
            .HasData(new SecuritySettings
            {
                Id = 1,
                MinimumPasswordLength = 15,
                EnforceSequencePolicy = false,
                PasswordExpirationDays = 90,
                SessionIdleTimeoutMinutes = 20,
                OneTimePasswordValidityHours = 24
            });
    }
}