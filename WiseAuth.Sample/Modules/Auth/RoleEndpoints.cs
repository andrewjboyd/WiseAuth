using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WiseAuth.Sample.Data;

namespace WiseAuth.Sample.Modules.Roles;

public static class RoleEndpoints
{
    public static void MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/roles");

        group.MapGet("", async (RoleManager<IdentityRole> roleManager, IWiseAuthService wiseAuth) =>
            {
                var claimTypes = wiseAuth.GetDetails().Keys.ToArray();
                var roles = await roleManager.Roles.OrderBy(r => r.Name).ToListAsync();

                var summaries = new List<RoleSummary>();
                foreach (var role in roles)
                {
                    var permissions = await GetRolePermissionsAsync(roleManager, role, claimTypes);
                    summaries.Add(new RoleSummary(role.Id, role.Name!, permissions));
                }

                return Results.Ok(summaries);
            })
            .EndpointId(RolePermissions.View);

        group.MapGet("/{id}", async (string id, RoleManager<IdentityRole> roleManager, IWiseAuthService wiseAuth) =>
            {
                var role = await roleManager.FindByIdAsync(id);
                if (role is null)
                {
                    return Results.NotFound();
                }

                var permissions = await GetRolePermissionsAsync(roleManager, role, wiseAuth.GetDetails().Keys.ToArray());
                return Results.Ok(new RoleSummary(role.Id, role.Name!, permissions));
            })
            .EndpointId(RolePermissions.View);

        group.MapPost("", async (CreateRoleRequest request, ClaimsPrincipal principal, RoleManager<IdentityRole> roleManager, IWiseAuthService wiseAuth) =>
            {
                var escalationErrors = GetEscalationErrors(principal, request.Permissions);
                if (escalationErrors is not null)
                {
                    return Results.BadRequest(escalationErrors);
                }

                var role = new IdentityRole(request.Name);

                var createResult = await roleManager.CreateAsync(role);
                if (!createResult.Succeeded)
                {
                    return Results.BadRequest(createResult.Errors.Select(e => e.Description));
                }

                await AddRolePermissionClaimsAsync(roleManager, role, request.Permissions, wiseAuth.GetDetails().Keys);

                var permissions = await GetRolePermissionsAsync(roleManager, role, wiseAuth.GetDetails().Keys.ToArray());
                var summary = new RoleSummary(role.Id, role.Name!, permissions);
                return Results.Created($"/api/roles/{role.Id}", summary);
            })
            .EndpointId(RolePermissions.Manage);

        group.MapPut("/{id}/permissions", async (string id, UpdateRolePermissionsRequest request, ClaimsPrincipal principal, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, IWiseAuthService wiseAuth) =>
            {
                var escalationErrors = GetEscalationErrors(principal, request.Permissions);
                if (escalationErrors is not null)
                {
                    return Results.BadRequest(escalationErrors);
                }

                var role = await roleManager.FindByIdAsync(id);
                if (role is null)
                {
                    return Results.NotFound();
                }

                // Prevents a member of this role from editing its own permissions - e.g.
                // revoking the role's Manage bit and locking every member (including
                // themselves) out. Combined with GetEscalationErrors above (which already
                // stops a self-edit from *granting* more than the caller holds), this
                // closes the remaining self-lockout/self-tinkering path.
                var currentUser = await userManager.GetUserAsync(principal);
                if (currentUser is not null && await userManager.IsInRoleAsync(currentUser, role.Name!))
                {
                    return Results.BadRequest(new[] { "You cannot edit the permissions of a role you belong to." });
                }

                var claimTypes = wiseAuth.GetDetails().Keys.ToArray();
                // Only claim types present in the request are replaced - an omitted key
                // leaves that permission untouched instead of silently revoking it. To
                // revoke a permission, the client must submit it explicitly as 0.
                var submittedTypes = request.Permissions?.Keys.ToHashSet() ?? new HashSet<string>();
                var existingClaims = await roleManager.GetClaimsAsync(role);
                var claimsToRemove = existingClaims.Where(c => submittedTypes.Contains(c.Type)).ToList();
                foreach (var claim in claimsToRemove)
                {
                    await roleManager.RemoveClaimAsync(role, claim);
                }

                await AddRolePermissionClaimsAsync(roleManager, role, request.Permissions, claimTypes);

                var permissions = await GetRolePermissionsAsync(roleManager, role, claimTypes);
                return Results.Ok(new RoleSummary(role.Id, role.Name!, permissions));
            })
            .EndpointId(RolePermissions.Manage);
    }

    // Prevents a caller from granting permissions they don't hold themselves - e.g. a
    // user with only RolePermissions.Manage creating a role that carries
    // UserPermissions.Manage, which anyone later assigned that role would inherit.
    private static List<string>? GetEscalationErrors(ClaimsPrincipal principal, Dictionary<string, ulong>? permissions)
    {
        if (permissions is null)
        {
            return null;
        }

        var errors = new List<string>();
        foreach (var (claimType, value) in permissions)
        {
            var callerClaim = principal.FindFirst(claimType);
            var callerValue = callerClaim is not null && ulong.TryParse(callerClaim.Value, out var parsed) ? parsed : 0UL;
            if ((value & ~callerValue) != 0)
            {
                errors.Add($"You cannot grant '{claimType}' permissions you do not hold yourself.");
            }
        }

        return errors.Count > 0 ? errors : null;
    }

    private static async Task AddRolePermissionClaimsAsync(
        RoleManager<IdentityRole> roleManager,
        IdentityRole role,
        Dictionary<string, ulong>? permissions,
        IEnumerable<string> registeredClaimTypes)
    {
        if (permissions is null)
        {
            return;
        }

        foreach (var claimType in registeredClaimTypes)
        {
            if (permissions.TryGetValue(claimType, out var value) && value != 0)
            {
                await roleManager.AddClaimAsync(role, new Claim(claimType, value.ToString()));
            }
        }
    }

    private static async Task<Dictionary<string, ulong>> GetRolePermissionsAsync(
        RoleManager<IdentityRole> roleManager,
        IdentityRole role,
        IReadOnlyCollection<string> claimTypes)
    {
        var permissions = claimTypes.ToDictionary(t => t, _ => 0UL);
        var claims = await roleManager.GetClaimsAsync(role);
        foreach (var claim in claims)
        {
            if (permissions.ContainsKey(claim.Type) && ulong.TryParse(claim.Value, out var value))
            {
                permissions[claim.Type] = value;
            }
        }

        return permissions;
    }
}
