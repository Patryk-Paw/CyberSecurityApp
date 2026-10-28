using CyberSecurityApp.Data;
using CyberSecurityApp.Models;
using CyberSecurityApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services
    .AddIdentity<User, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 15;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;

        options.User.RequireUniqueEmail = false;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<PasswordPolicyService>();
builder.Services.AddScoped<OneTimePasswordGenerator>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
    options.SlidingExpiration = true;
    options.Cookie.IsEssential = true;
    options.Cookie.MaxAge = null;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<User>>();
    var context = services.GetRequiredService<ApplicationDbContext>();

    await context.Database.MigrateAsync();

    string[] roles =
    [
        "Administrator",
        "Redaktor",
        "Czytelnik"
    ];

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(role));

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    roleResult.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"Nie uda³o siê utworzyæ roli {role}: {errors}");
            }
        }
    }

    var admin = await userManager.FindByNameAsync("Admin");

    if (admin == null)
    {
        admin = new User
        {
            UserName = "Admin",
            EmailConfirmed = true,
            IsActive = true,
            MustChangePassword = false,
            CreatedAt = DateTime.UtcNow
        };

        var passwordHasher = services.GetRequiredService<IPasswordHasher<User>>();

        admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin");

        var createResult = await userManager.CreateAsync(admin);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(
                "; ",
                createResult.Errors.Select(error => error.Description));

            throw new InvalidOperationException(
                $"Nie uda³o siê utworzyæ konta testowego Admin: {errors}");
        }
    }

    if (!await userManager.IsInRoleAsync(admin, "Administrator"))
    {
        var roleResult = await userManager.AddToRoleAsync(admin, "Administrator");

        if (!roleResult.Succeeded)
        {
            var errors = string.Join(
                "; ",
                roleResult.Errors.Select(error => error.Description));

            throw new InvalidOperationException(
                $"Nie uda³o siê przypisaæ roli Administrator: {errors}");
        }
    }

    var users = await context.Users
    .AsNoTracking()
    .Select(user => new
    {
        user.UserName,
        user.PasswordHash,
        user.IsActive,
        user.PasswordChangedAt
    })
    .ToListAsync();

    Console.WriteLine();
    Console.WriteLine("=== USERS AND PASSWORD HASHES ===");

    foreach (var user in users)
    {
        Console.WriteLine($"Username: {user.UserName}");
        Console.WriteLine($"Password hash: {user.PasswordHash ?? "(no hash)"}");
        Console.WriteLine($"Active: {user.IsActive}");
        Console.WriteLine($"Password changed at: {user.PasswordChangedAt}");
        Console.WriteLine("--------------------------------");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userManager = context.RequestServices
            .GetRequiredService<UserManager<User>>();

        var user = await userManager.GetUserAsync(context.User);

        if (user == null || !user.IsActive)
        {
            await context.SignOutAsync(IdentityConstants.ApplicationScheme);
            context.Response.Redirect("/Account/Login");
            return;
        }


        if (user != null)
        {
            var requiresPasswordChange = user.MustChangePassword;

            if (!requiresPasswordChange && user.PasswordChangedAt.HasValue)
            {
                var dbContext = context.RequestServices
                    .GetRequiredService<ApplicationDbContext>();

                var settings = await dbContext.SecuritySettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(setting => setting.Id == 1);

                var expirationDays = settings?.PasswordExpirationDays ?? 90;

                requiresPasswordChange =
                    user.PasswordChangedAt.Value.AddDays(expirationDays)
                    <= DateTime.UtcNow;
            }

            if (requiresPasswordChange)
            {
                var path = context.Request.Path;

                var allowedPaths = new[]
                {
            "/Password/ChangePassword",
            "/Account/Logout",
            "/Account/AccessDenied"
        };

                var isAllowed = allowedPaths.Any(allowedPath =>
                    path.StartsWithSegments(
                        allowedPath,
                        StringComparison.OrdinalIgnoreCase));

                if (!isAllowed)
                {
                    context.Response.Redirect("/Password/ChangePassword");
                    return;
                }
            }
        }

    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
