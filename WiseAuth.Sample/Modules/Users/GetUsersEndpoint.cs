using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WiseAuth.Sample.Data;

namespace WiseAuth.Sample.Modules.Users;

public sealed class GetUsersEndpoint(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IWiseAuthService wiseAuth) : EndpointWithoutRequest<List<UserSummary>>
{
    public override void Configure()
    {
        Get("/api/users");
        Options(b => b.EndpointId(UserPermissions.View));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var claimTypes = wiseAuth.GetDetails().Keys.ToArray();
        var users = await userManager.Users.OrderBy(u => u.UserName).ToListAsync(ct);

        var summaries = new List<UserSummary>();
        foreach (var user in users)
        {
            var permissions = await UserEndpointHelpers.GetEffectivePermissionsAsync(signInManager, user, claimTypes);
            summaries.Add(new UserSummary(user.Id, user.UserName!, user.DisplayName, user.Email!, permissions));
        }

        await Send.OkAsync(summaries, ct);
    }
}
