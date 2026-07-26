using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using WiseAuth.Sample.Data;

namespace WiseAuth.Sample.Modules.Users;

public sealed class UpdateUserClaimsEndpoint(UserManager<ApplicationUser> userManager, IWiseAuthService wiseAuth)
    : Endpoint<UpdateUserClaimsRequest, Dictionary<string, ulong>>
{
    public override void Configure()
    {
        Put("/api/users/{id}/claims");
        Options(b => b.EndpointId(UserPermissions.Manage));
    }

    public override async Task HandleAsync(UpdateUserClaimsRequest req, CancellationToken ct)
    {
        // Prevents a user from editing their own claims - e.g. revoking their own Manage bit and
        // locking themselves out, or granting themselves more access. Checked here (not just
        // hidden client-side) since this is a business rule, not something WiseAuth's claim-bitmask
        // check can express.
        if (string.Equals(userManager.GetUserId(User), req.Id, StringComparison.Ordinal))
        {
            await Send.ResultAsync(Results.BadRequest(new[] { "You cannot edit your own claims." }));
            return;
        }

        var escalationErrors = UserEndpointHelpers.GetEscalationErrors(User, req.Permissions);
        if (escalationErrors is not null)
        {
            await Send.ResultAsync(Results.BadRequest(escalationErrors));
            return;
        }

        var user = await userManager.FindByIdAsync(req.Id);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var claimTypes = wiseAuth.GetDetails().Keys.ToArray();
        // Only claim types present in the request are replaced - an omitted key leaves that
        // permission untouched instead of silently revoking it. To revoke a permission, the client
        // must submit it explicitly as 0.
        var submittedTypes = req.Permissions?.Keys.ToHashSet() ?? new HashSet<string>();
        var existingClaims = await userManager.GetClaimsAsync(user);
        var claimsToRemove = existingClaims.Where(c => submittedTypes.Contains(c.Type)).ToList();
        if (claimsToRemove.Count > 0)
        {
            await userManager.RemoveClaimsAsync(user, claimsToRemove);
        }

        await UserEndpointHelpers.AddClaimsAsync(userManager, user, req.Permissions, claimTypes);

        var permissions = await UserEndpointHelpers.GetRawClaimsAsync(userManager, user, claimTypes);
        await Send.OkAsync(permissions, ct);
    }
}
