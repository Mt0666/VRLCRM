using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VRLCRM.Domain.Entities;
using VRLCRM.Infrastructure.Options;

namespace VRLCRM.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SeedData");
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var seedOptions = serviceProvider.GetRequiredService<IOptions<SeedDataOptions>>().Value;

        await context.Database.MigrateAsync();

        await context.Customers
            .Where(c => c.CreditLimit == null)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.CreditLimit, 0m));

        await MockDataSeeder.SeedAsync(context);

        foreach (var roleName in seedOptions.DefaultRoles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var roleResult = await roleManager.CreateAsync(new ApplicationRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
                Description = $"{roleName} role"
            });

            if (!roleResult.Succeeded)
            {
                logger.LogError("Role '{RoleName}' could not be created.", roleName);
            }
        }

        if (string.IsNullOrWhiteSpace(seedOptions.AdminEmail) ||
            string.IsNullOrWhiteSpace(seedOptions.AdminPassword))
        {
            logger.LogWarning("Seed admin user skipped. Configure SeedData:AdminEmail and SeedData:AdminPassword.");
            return;
        }

        var adminUser = await userManager.FindByEmailAsync(seedOptions.AdminEmail);
        if (adminUser is not null)
        {
            return;
        }

        adminUser = new ApplicationUser
        {
            UserName = seedOptions.AdminEmail,
            Email = seedOptions.AdminEmail,
            EmailConfirmed = true,
            FullName = "System Administrator"
        };

        var createResult = await userManager.CreateAsync(adminUser, seedOptions.AdminPassword);
        if (!createResult.Succeeded)
        {
            logger.LogError("Admin user could not be created.");
            return;
        }

        var adminRoleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
        if (!adminRoleResult.Succeeded)
        {
            logger.LogError("Admin user could not be assigned to Admin role.");
        }
    }
}
