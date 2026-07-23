using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace WiseAuth;

// Deriving from AuthorizationHandler<TRequirement> (rather than raw IAuthorizationHandler) pulls
// the requirement straight from context.PendingRequirements, so this works no matter how the
// requirement was attached - minimal APIs via WiseAuthHelpers.EndpointId's WithMetadata call, or
// controller actions via EndpointIdAttribute<T>'s IAuthorizationRequirementData.GetRequirements.
// An HttpContext.GetEndpoint() metadata lookup (the previous approach) only sees the latter as
// the attribute instance, never the WiseAuthMetadata<T> requirement itself, so it can't be used
// to support both hosting models uniformly.
internal sealed class WiseAuthorizationHandler<T>(ILogger<WiseAuthorizationHandler<T>> logger) : AuthorizationHandler<WiseAuthMetadata<T>>
    where T : Enum
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, WiseAuthMetadata<T> requirement)
    {
        logger.LogInformation("Evaluating authorization requirement for EndPointId: {MetadataEndpointId}", requirement.EndpointId);

        var authClaim = context.User.FindFirst(c => c.Type.Equals(WiseAuthMetadata<T>.ClaimType, StringComparison.OrdinalIgnoreCase));
        if (authClaim is null)
        {
            return Task.CompletedTask;
        }

        if (!ulong.TryParse(authClaim.Value, out var endPointPermissions))
        {
            logger.LogError("Could not convert '{AuthClaimValue}' to a long for claim type {AuthClaimType}", authClaim.Value, authClaim.Type);
            return Task.CompletedTask;
        }

        if ((endPointPermissions & requirement.EndpointId) != 0)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
