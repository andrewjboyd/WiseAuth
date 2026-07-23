using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using WiseAuth.Sample.Modules.Products;
using WiseAuth.Sample.Modules.Roles;
using WiseAuth.Sample.Modules.Users;

namespace WiseAuth.Sample.Data;

public static class DbSeeder
{
    private const string ProductsClaimType = "products";
    private const string UsersClaimType = "users";
    private const string RolesClaimType = "roles";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.EnsureCreatedAsync();

        await SeedRoleAsync(roleManager, "Admins", new Dictionary<string, ulong>
        {
            [ProductsClaimType] = AllPermissions<ProductPermissions>(),
            [UsersClaimType] = AllPermissions<UserPermissions>(),
            [RolesClaimType] = AllPermissions<RolePermissions>(),
        });
        await SeedRoleAsync(roleManager, "Viewers", new Dictionary<string, ulong>
        {
            [ProductsClaimType] = (ulong)ProductPermissions.Read,
        });

        // Admin's full access comes entirely from the Admins role - zero personal claims.
        var admin = await SeedUserAsync(userManager, "Admin", "Admin123!", "Admin", permissions: null);
        await AssignRoleAsync(userManager, admin, "Admins");

        // Viewer's baseline Read-only access comes entirely from the Viewers role.
        var viewer = await SeedUserAsync(userManager, "Viewer", "Viewer123!", "Viewer", permissions: null);
        await AssignRoleAsync(userManager, viewer, "Viewers");

        // Auditor demonstrates the additive merge: Viewers grants products=Read, and a
        // personal claim adds Export on top - effective products = Read|Export, without
        // granting Export to every Viewer.
        var auditor = await SeedUserAsync(userManager, "Auditor", "Auditor123!", "Auditor", new Dictionary<string, ulong>
        {
            [ProductsClaimType] = (ulong)ProductPermissions.Export,
        });
        await AssignRoleAsync(userManager, auditor, "Viewers");

        if (!db.Products.Any())
        {
            db.Products.AddRange(
                new Product { Sku = "WA-001", Name = "Widget", Price = 9.99m, Quantity = 100 },
                new Product { Sku = "WA-002", Name = "Gadget", Price = 19.99m, Quantity = 50 },
                new Product { Sku = "WA-003", Name = "Doohickey", Price = 4.50m, Quantity = 250 });
            await db.SaveChangesAsync();
        }
    }

    private static ulong AllPermissions<T>() where T : struct, Enum =>
        Enum.GetValues<T>().Aggregate(0UL, (acc, value) => acc | Convert.ToUInt64(value));

    private static async Task SeedRoleAsync(RoleManager<IdentityRole> roleManager, string name, Dictionary<string, ulong> permissions)
    {
        if (await roleManager.FindByNameAsync(name) is not null)
        {
            return;
        }

        var role = new IdentityRole(name);
        var createResult = await roleManager.CreateAsync(role);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed role '{name}': {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
        }

        foreach (var (claimType, value) in permissions)
        {
            if (value != 0)
            {
                await roleManager.AddClaimAsync(role, new Claim(claimType, value.ToString()));
            }
        }
    }

    private static async Task<ApplicationUser> SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string userName,
        string password,
        string displayName,
        Dictionary<string, ulong>? permissions)
    {
        var existing = await userManager.FindByNameAsync(userName);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = $"{userName.ToLowerInvariant()}@wiseauth.sample",
            EmailConfirmed = true,
            DisplayName = displayName,
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed user '{userName}': {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
        }

        if (permissions is not null)
        {
            foreach (var (claimType, value) in permissions)
            {
                if (value != 0)
                {
                    await userManager.AddClaimAsync(user, new Claim(claimType, value.ToString()));
                }
            }
        }

        return user;
    }

    private static async Task AssignRoleAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, string roleName)
    {
        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            await userManager.AddToRoleAsync(user, roleName);
        }
    }
}
