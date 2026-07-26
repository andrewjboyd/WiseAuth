using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WiseAuth.Sample.Data;

namespace WiseAuth.Sample.Modules.Users;

public sealed class GetUserRolesEndpoint(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    : Endpoint<UserIdRequest, UserRolesResponse>
{
    public override void Configure()
    {
        Get("/api/users/{id}/roles");
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

        var memberRoleNames = await userManager.GetRolesAsync(user);
        var allRoleNames = await roleManager.Roles.Select(r => r.Name!).ToListAsync(ct);
        await Send.OkAsync(new UserRolesResponse(memberRoleNames.ToArray(), allRoleNames.ToArray()), ct);
    }
}
