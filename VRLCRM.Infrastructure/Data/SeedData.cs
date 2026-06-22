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

        // PurchasePrice kolonu migration dışında eklendiğinde güvenlik ağı
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'dbo'
                  AND TABLE_NAME  = 'StockItems'
                  AND COLUMN_NAME = 'PurchasePrice'
            )
            BEGIN
                ALTER TABLE [dbo].[StockItems]
                ADD [PurchasePrice] decimal(18,2) NOT NULL DEFAULT 0;
            END");

        // Invoices tablosuna iskonto kolonları
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'dbo'
                  AND TABLE_NAME  = 'Invoices'
                  AND COLUMN_NAME = 'DiscountRate'
            )
            BEGIN
                ALTER TABLE [dbo].[Invoices]
                ADD [DiscountRate] decimal(18,2) NOT NULL DEFAULT 0;
            END

            IF NOT EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = 'dbo'
                  AND TABLE_NAME  = 'Invoices'
                  AND COLUMN_NAME = 'DiscountAmount'
            )
            BEGIN
                ALTER TABLE [dbo].[Invoices]
                ADD [DiscountAmount] decimal(18,2) NOT NULL DEFAULT 0;
            END");

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
