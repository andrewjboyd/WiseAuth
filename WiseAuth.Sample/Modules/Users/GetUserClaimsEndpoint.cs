using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using WiseAuth.Sample.Data;

namespace WiseAuth.Sample.Modules.Users;

public sealed class GetUserClaimsEndpoint(UserManager<ApplicationUser> userManager, IWiseAuthService wiseAuth)
    : Endpoint<UserIdRequest, Dictionary<string, ulong>>
{
    public override void Configure()
    {
        Get("/api/users/{id}/claims");
        Options(b => b.EndpointId(UserPermissions.View));
    }

    public override async Task HandleAsync(UserIdRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(req.Id);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var claimTypes = wiseAuth.GetDetails().Keys.ToArray();
        var permissions = await UserEndpointHelpers.GetRawClaimsAsync(userManager, user, claimTypes);
        await Send.OkAsync(permissions, ct);
    }
}
