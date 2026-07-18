using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using WiseAuth.Sample.Data;

namespace WiseAuth.Sample.Security;

// ASP.NET Identity's default factory adds the user's own claims and each of their
// roles' claims as separate, non-deduplicated Claim objects - a user in a role and
// holding a personal claim of the same type ends up with two claims of that type on
// their principal. WiseAuthorizationHandler<T> only reads the first match via
// FindFirst, so without this merge step, effective permissions would silently depend
// on claim insertion order rather than being the OR of every source, as intended.
public sealed class MergingUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<IdentityOptions> optionsAccessor,
    IWiseAuthService wiseAuth)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(userManager, roleManager, optionsAccessor)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        foreach (var claimType in wiseAuth.GetDetails().Keys)
        {
            var matching = identity.FindAll(c => c.Type.Equals(claimType, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matching.Count == 0)
            {
                continue;
            }

            var merged = matching.Aggregate(0UL, (acc, c) => ulong.TryParse(c.Value, out var value) ? acc | value : acc);
            foreach (var claim in matching)
            {
                identity.RemoveClaim(claim);
            }

            identity.AddClaim(new Claim(claimType, merged.ToString()));
        }

        return identity;
    }
}
