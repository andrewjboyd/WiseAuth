using Microsoft.AspNetCore.Authorization;

namespace WiseAuth;

/// <summary>
/// Attribute-based equivalent of <see cref="WiseAuthHelpers.EndpointId{T}"/> for controller actions.
/// Inheriting <see cref="AuthorizeAttribute"/> makes the endpoint require authorization (matching
/// the minimal-API extension's implicit <c>RequireAuthorization()</c> call), while
/// <see cref="IAuthorizationRequirementData"/> attaches the same <see cref="WiseAuthMetadata{T}"/>
/// requirement that <see cref="WiseAuthorizationHandler{T}"/> already knows how to evaluate.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class EndpointIdAttribute<T> : AuthorizeAttribute, IAuthorizationRequirementData
    where T : Enum
{
    private readonly T _endpointId;

    public EndpointIdAttribute(T endpointId)
    {
        _endpointId = endpointId;
    }

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return new WiseAuthMetadata<T>(_endpointId);
    }
}
