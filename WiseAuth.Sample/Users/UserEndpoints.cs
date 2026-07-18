using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WiseAuth;
using WiseAuth.Sample.Data;

namespace WiseAuth.Sample.Users;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users");

        group.MapGet("", async (UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IWiseAuthService wiseAuth) =>
            {
                var claimTypes = wiseAuth.GetDetails().Keys.ToArray();
                var users = await userManager.Users.OrderBy(u => u.UserName).ToListAsync();

                var summaries = new List<UserSummary>();
                foreach (var user in users)
                {
                    var permissions = await GetEffectivePermissionsAsync(signInManager, user, claimTypes);
                    summaries.Add(new UserSummary(user.Id, user.UserName!, user.DisplayName, user.Email!, permissions));
                }

                return Results.Ok(summaries);
            })
            .EndpointId(UserPermissions.View);

        group.MapGet("/{id}", async (string id, UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    return Results.NotFound();
                }

                return Results.Ok(new UserDetail(user.Id, user.UserName!, user.DisplayName, user.Email!));
            })
            .EndpointId(UserPermissions.View);

        // No self-edit block here - unlike claims/roles, changing your own username,
        // display name, or email isn't a privilege-escalation vector, so there's no
        // business rule to enforce beyond the usual Manage gate.
        group.MapPut("/{id}/profile", async (string id, UpdateUserProfileRequest request, UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    return Results.NotFound();
                }

                var usernameResult = await userManager.SetUserNameAsync(user, request.UserName);
                if (!usernameResult.Succeeded)
                {
                    return Results.BadRequest(usernameResult.Errors.Select(e => e.Description));
                }

                var emailResult = await userManager.SetEmailAsync(user, request.Email);
                if (!emailResult.Succeeded)
                {
                    return Results.BadRequest(emailResult.Errors.Select(e => e.Description));
                }

                user.DisplayName = request.DisplayName;
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return Results.BadRequest(updateResult.Errors.Select(e => e.Description));
                }

                return Results.Ok(new UserDetail(user.Id, user.UserName!, user.DisplayName, user.Email!));
            })
            .EndpointId(UserPermissions.Manage);

        group.MapPost("", async (CreateUserRequest request, ClaimsPrincipal principal, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IWiseAuthService wiseAuth) =>
            {
                var escalationErrors = GetEscalationErrors(principal, request.Permissions);
                if (escalationErrors is not null)
                {
                    return Results.BadRequest(escalationErrors);
                }

                var user = new ApplicationUser
                {
                    UserName = request.UserName,
                    Email = request.Email,
                    EmailConfirmed = true,
                    DisplayName = request.DisplayName,
                };

                var createResult = await userManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    return Results.BadRequest(createResult.Errors.Select(e => e.Description));
                }

                await AddClaimsAsync(userManager, user, request.Permissions, wiseAuth.GetDetails().Keys);

                var permissions = await GetEffectivePermissionsAsync(signInManager, user, wiseAuth.GetDetails().Keys.ToArray());
                var summary = new UserSummary(user.Id, user.UserName!, user.DisplayName, user.Email!, permissions);
                return Results.Created($"/api/users/{user.Id}", summary);
            })
            .EndpointId(UserPermissions.Manage);

        group.MapGet("/{id}/claims", async (string id, UserManager<ApplicationUser> userManager, IWiseAuthService wiseAuth) =>
            {
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    return Results.NotFound();
                }

                var claimTypes = wiseAuth.GetDetails().Keys.ToArray();
                var permissions = await GetRawClaimsAsync(userManager, user, claimTypes);
                return Results.Ok(permissions);
            })
            .EndpointId(UserPermissions.View);

        group.MapPut("/{id}/claims", async (string id, UpdateUserClaimsRequest request, ClaimsPrincipal principal, UserManager<ApplicationUser> userManager, IWiseAuthService wiseAuth) =>
            {
                // Prevents a user from editing their own claims - e.g. revoking their own
                // Manage bit and locking themselves out, or granting themselves more access.
                // Checked here (not just hidden client-side) since this is a business rule, not
                // something WiseAuth's claim-bitmask check can express.
                if (string.Equals(userManager.GetUserId(principal), id, StringComparison.Ordinal))
                {
                    return Results.BadRequest(new[] { "You cannot edit your own claims." });
                }

                var escalationErrors = GetEscalationErrors(principal, request.Permissions);
                if (escalationErrors is not null)
                {
                    return Results.BadRequest(escalationErrors);
                }

                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    return Results.NotFound();
                }

                var claimTypes = wiseAuth.GetDetails().Keys.ToArray();
                // Only claim types present in the request are replaced - an omitted key
                // leaves that permission untouched instead of silently revoking it. To
                // revoke a permission, the client must submit it explicitly as 0.
                var submittedTypes = request.Permissions?.Keys.ToHashSet() ?? new HashSet<string>();
                var existingClaims = await userManager.GetClaimsAsync(user);
                var claimsToRemove = existingClaims.Where(c => submittedTypes.Contains(c.Type)).ToList();
                if (claimsToRemove.Count > 0)
                {
                    await userManager.RemoveClaimsAsync(user, claimsToRemove);
                }

                await AddClaimsAsync(userManager, user, request.Permissions, claimTypes);

                var permissions = await GetRawClaimsAsync(userManager, user, claimTypes);
                return Results.Ok(permissions);
            })
            .EndpointId(UserPermissions.Manage);

        group.MapGet("/{id}/roles", async (string id, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) =>
            {
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    return Results.NotFound();
                }

                var memberRoleNames = await userManager.GetRolesAsync(user);
                var allRoleNames = await roleManager.Roles.Select(r => r.Name!).ToListAsync();
                return Results.Ok(new UserRolesResponse(memberRoleNames.ToArray(), allRoleNames.ToArray()));
            })
            .EndpointId(UserPermissions.View);

        group.MapPut("/{id}/roles", async (string id, UpdateUserRolesRequest request, ClaimsPrincipal principal, UserManager<ApplicationUser> userManager) =>
            {
                // Same rationale as the claims self-edit block above - without this, a user
                // blocked from editing their own claims could just add themselves to a
                // powerful role instead and bypass the restriction entirely.
                if (string.Equals(userManager.GetUserId(principal), id, StringComparison.Ordinal))
                {
                    return Results.BadRequest(new[] { "You cannot edit your own role memberships." });
                }

                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    return Results.NotFound();
                }

                var currentRoles = await userManager.GetRolesAsync(user);
                if (currentRoles.Count > 0)
                {
                    await userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                var addResult = await userManager.AddToRolesAsync(user, request.RoleNames);
                if (!addResult.Succeeded)
                {
                    return Results.BadRequest(addResult.Errors.Select(e => e.Description));
                }

                var memberRoleNames = await userManager.GetRolesAsync(user);
                return Results.Ok(memberRoleNames);
            })
            .EndpointId(UserPermissions.Manage);
    }

    // Prevents a caller from granting permissions they don't hold themselves - e.g. a
    // user with only UserPermissions.Manage handing out RolePermissions.Manage or
    // ProductPermissions.Delete to another user via the Permissions payload, which
    // AddClaimsAsync would otherwise apply for every registered claim type with no
    // regard for what the caller is actually entitled to grant.
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

    private static async Task AddClaimsAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
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
                await userManager.AddClaimAsync(user, new Claim(claimType, value.ToString()));
            }
        }
    }

    private static async Task<Dictionary<string, ulong>> GetRawClaimsAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        IReadOnlyCollection<string> claimTypes)
    {
        var permissions = claimTypes.ToDictionary(t => t, _ => 0UL);
        var claims = await userManager.GetClaimsAsync(user);
        foreach (var claim in claims)
        {
            if (permissions.ContainsKey(claim.Type) && ulong.TryParse(claim.Value, out var value))
            {
                permissions[claim.Type] = value;
            }
        }

        return permissions;
    }

    private static async Task<Dictionary<string, ulong>> GetEffectivePermissionsAsync(
        SignInManager<ApplicationUser> signInManager,
        ApplicationUser user,
        IReadOnlyCollection<string> claimTypes)
    {
        var principal = await signInManager.CreateUserPrincipalAsync(user);
        var permissions = claimTypes.ToDictionary(t => t, _ => 0UL);
        foreach (var claimType in claimTypes)
        {
            var claim = principal.FindFirst(claimType);
            if (claim is not null && ulong.TryParse(claim.Value, out var value))
            {
                permissions[claimType] = value;
            }
        }

        return permissions;
    }
}
