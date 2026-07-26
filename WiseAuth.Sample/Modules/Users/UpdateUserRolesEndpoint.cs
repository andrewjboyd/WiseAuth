using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using WiseAuth.Sample.Data;

namespace WiseAuth.Sample.Modules.Users;

public sealed class UpdateUserRolesEndpoint(UserManager<ApplicationUser> userManager)
    : Endpoint<UpdateUserRolesRequest, string[]>
{
    public override void Configure()
    {
        Put("/api/users/{id}/roles");
        Options(b => b.EndpointId(UserPermissions.Manage));
    }

    public override async Task HandleAsync(UpdateUserRolesRequest req, CancellationToken ct)
    {
        // Same rationale as the claims self-edit block above - without this, a user blocked from
        // editing their own claims could just add themselves to a powerful role instead and bypass
        // the restriction entirely.
        if (string.Equals(userManager.GetUserId(User), req.Id, StringComparison.Ordinal))
        {
            await Send.ResultAsync(Results.BadRequest(new[] { "You cannot edit your own role memberships." }));
            return;
        }

        var user = await userManager.FindByIdAsync(req.Id);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        var addResult = await userManager.AddToRolesAsync(user, req.RoleNames);
        if (!addResult.Succeeded)
        {
            await Send.ResultAsync(Results.BadRequest(addResult.Errors.Select(e => e.Description)));
            return;
        }

        var memberRoleNames = await userManager.GetRolesAsync(user);
        await Send.OkAsync(memberRoleNames.ToArray(), ct);
    }
}
