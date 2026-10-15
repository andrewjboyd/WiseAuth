using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace WiseAuth;

internal class WiseAuthDetails<T>
    : IWiseAuthDetails where T : struct, Enum
{
    public WiseAuthDetails()
    {
        ClaimType = WiseAuthHelpers.GetClaimType(typeof(T));
        var enumNames = Enum.GetNames<T>();
        var enumValues = Enum.GetValues<T>();

        var details = enumNames
            .Select((name, idx) =>
            {
                var display = typeof(T).GetField(name)?.GetCustomAttribute<DisplayAttribute>();
                return new EndpointDetail(Convert.ToUInt64(enumValues[idx]), name,
                    display?.GetName(), display?.GetDescription());
            })
            .ToList();
        EndpointDetails = [.. details];
    }

    public string ClaimType { get; }
    public EndpointDetail[] EndpointDetails { get; }
}
