using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace WiseAuth;

internal sealed class WiseAuthorizationHandler<T>(ILogger<WiseAuthorizationHandler<T>> logger) : IAuthorizationHandler
    where T : Enum
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            return Task.CompletedTask;
        }

        var metadata = httpContext.GetEndpoint()?.Metadata.GetMetadata<WiseAuthMetadata<T>>();
        if (metadata is null)
        {
            return Task.CompletedTask;
        }

        logger.LogInformation("Evaluating authorization requirement for EndPointId: {MetadataEndpointId}", metadata.EndpointId);
        
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

        var endPointIdentifier = metadata.EndpointId;
        if ((endPointPermissions & endPointIdentifier) != 0)
        {
            context.Succeed(metadata);
        }

        return Task.CompletedTask;
    }
}