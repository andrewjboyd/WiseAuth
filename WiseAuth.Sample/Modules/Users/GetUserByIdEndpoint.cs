using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using WiseAuth.Sample.Data;

namespace WiseAuth.Sample.Modules.Users;

public sealed class GetUserByIdEndpoint(UserManager<ApplicationUser> userManager)
    : Endpoint<UserIdRequest, UserDetail>
{
    public override void Configure()
    {
        Get("/api/users/{id}");
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

        await Send.OkAsync(new UserDetail(user.Id, user.UserName!, user.DisplayName, user.Email!), ct);
    }
}
