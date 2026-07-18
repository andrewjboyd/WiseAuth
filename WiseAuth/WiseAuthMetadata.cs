using Microsoft.AspNetCore.Authorization;

namespace WiseAuth;

internal sealed class WiseAuthMetadata<T>(T endpointId) : IAuthorizationRequirement, IAuthorizationRequirementData
    where T : Enum
{
    public ulong EndpointId { get; } = Convert.ToUInt64(endpointId);

    public static string ClaimType => WiseAuthHelpers.GetClaimType(typeof(T));

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return this;
    }
}