using Microsoft.AspNetCore.Authorization;

namespace WiseAuth;

internal sealed class WiseAuthMetadata<T>(T endpointId) : IAuthorizationRequirement
    where T : Enum
{
    public ulong EndpointId { get; } = Convert.ToUInt64(endpointId);
    
    public static string ClaimType => WiseAuthHelpers.GetClaimType(typeof(T));
}