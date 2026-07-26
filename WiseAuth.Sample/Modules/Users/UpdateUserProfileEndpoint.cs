using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using WiseAuth.Sample.Data;

namespace WiseAuth.Sample.Modules.Users;

public sealed class UpdateUserProfileEndpoint(UserManager<ApplicationUser> userManager)
    : Endpoint<UpdateUserProfileRequest, UserDetail>
{
    public override void Configure()
    {
        Put("/api/users/{id}/profile");
        Options(b => b.EndpointId(UserPermissions.Manage));
    }

    public override async Task HandleAsync(UpdateUserProfileRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByIdAsync(req.Id);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var usernameResult = await userManager.SetUserNameAsync(user, req.UserName);
        if (!usernameResult.Succeeded)
        {
            await Send.ResultAsync(Results.BadRequest(usernameResult.Errors.Select(e => e.Description)));
            return;
        }

        var emailResult = await userManager.SetEmailAsync(user, req.Email);
        if (!emailResult.Succeeded)
        {
            await Send.ResultAsync(Results.BadRequest(emailResult.Errors.Select(e => e.Description)));
            return;
        }

        user.DisplayName = req.DisplayName;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            await Send.ResultAsync(Results.BadRequest(updateResult.Errors.Select(e => e.Description)));
            return;
        }

        await Send.OkAsync(new UserDetail(user.Id, user.UserName!, user.DisplayName, user.Email!), ct);
    }
}
