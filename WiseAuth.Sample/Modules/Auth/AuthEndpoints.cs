using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using WiseAuth.Sample.Data;

namespace WiseAuth.Sample.Modules.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        // BFF pattern: the browser only ever receives an HttpOnly cookie. The raw JWT
        // never appears in a response body or in browser-accessible storage.
        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWiseAuthService wiseAuth) =>
        {
            var user = await userManager.FindByNameAsync(request.UserName);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var passwordCheck = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
            if (!passwordCheck.Succeeded)
            {
                return Results.Unauthorized();
            }

            await signInManager.SignInAsync(user, isPersistent: true);

            // HttpContext.User isn't updated synchronously within this request after
            // SignInAsync, so re-derive the merged principal explicitly rather than
            // reading raw claims directly - this goes through the same
            // MergingUserClaimsPrincipalFactory that issues the cookie, so the response
            // reflects effective (role + claim) permissions, not just this user's own claims.
            var principal = await signInManager.CreateUserPrincipalAsync(user);
            var response = new AuthResponse(user.Id, user.DisplayName, ToPermissions(principal.Claims, wiseAuth.GetDetails().Keys));
            return Results.Ok(response);
        });

        group.MapGet("/me", async (ClaimsPrincipal principal, UserManager<ApplicationUser> userManager, IWiseAuthService wiseAuth) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            // Reads the live, already-merged HttpContext.User rather than re-deriving
            // permissions - this is literally what WiseAuthorizationHandler enforces right
            // now, which matters since the UI should never claim a permission the server
            // wouldn't currently honor.
            return Results.Ok(new AuthResponse(user.Id, user.DisplayName, ToPermissions(principal.Claims, wiseAuth.GetDetails().Keys)));
        }).RequireAuthorization();

        group.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Ok();
        });
    }

    // principal.Claims includes standard Identity claims (name identifier, security
    // stamp, role names, ...) alongside the merged permission claims - filter to only
    // WiseAuth-registered claim types before parsing, since those other claims' values
    // aren't valid ulongs.
    private static Dictionary<string, ulong> ToPermissions(IEnumerable<Claim> claims, IEnumerable<string> registeredClaimTypes)
    {
        var claimTypes = new HashSet<string>(registeredClaimTypes, StringComparer.OrdinalIgnoreCase);
        return claims
            .Where(c => claimTypes.Contains(c.Type))
            .ToDictionary(c => c.Type, c => ulong.Parse(c.Value));
    }
}
