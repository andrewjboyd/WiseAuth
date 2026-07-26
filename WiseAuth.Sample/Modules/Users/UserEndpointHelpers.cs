using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using WiseAuth.Sample.Data;

namespace WiseAuth.Sample.Modules.Users;

internal static class UserEndpointHelpers
{
    // Prevents a caller from granting permissions they don't hold themselves - e.g. a user with
    // only UserPermissions.Manage handing out RolePermissions.Manage or ProductPermissions.Delete
    // to another user via the Permissions payload, which AddClaimsAsync would otherwise apply for
    // every registered claim type with no regard for what the caller is actually entitled to grant.
    public static List<string>? GetEscalationErrors(ClaimsPrincipal principal, Dictionary<string, ulong>? permissions)
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

    public static async Task AddClaimsAsync(
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

    public static async Task<Dictionary<string, ulong>> GetRawClaimsAsync(
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

    public static async Task<Dictionary<string, ulong>> GetEffectivePermissionsAsync(
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
