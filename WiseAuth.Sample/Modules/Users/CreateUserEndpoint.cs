using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using WiseAuth.Sample.Data;

namespace WiseAuth.Sample.Modules.Users;

public sealed class CreateUserEndpoint(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IWiseAuthService wiseAuth) : Endpoint<CreateUserRequest, UserSummary>
{
    public override void Configure()
    {
        Post("/api/users");
        Options(b => b.EndpointId(UserPermissions.Manage));
    }

    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        var escalationErrors = UserEndpointHelpers.GetEscalationErrors(User, req.Permissions);
        if (escalationErrors is not null)
        {
            await Send.ResultAsync(Results.BadRequest(escalationErrors));
            return;
        }

        var user = new ApplicationUser
        {
            UserName = req.UserName,
            Email = req.Email,
            EmailConfirmed = true,
            DisplayName = req.DisplayName,
        };

        var createResult = await userManager.CreateAsync(user, req.Password);
        if (!createResult.Succeeded)
        {
            await Send.ResultAsync(Results.BadRequest(createResult.Errors.Select(e => e.Description)));
            return;
        }

        var claimTypes = wiseAuth.GetDetails().Keys;
        await UserEndpointHelpers.AddClaimsAsync(userManager, user, req.Permissions, claimTypes);

        var permissions = await UserEndpointHelpers.GetEffectivePermissionsAsync(signInManager, user, claimTypes.ToArray());
        var summary = new UserSummary(user.Id, user.UserName!, user.DisplayName, user.Email!, permissions);
        await Send.ResultAsync(Results.Created($"/api/users/{user.Id}", summary));
    }
}
