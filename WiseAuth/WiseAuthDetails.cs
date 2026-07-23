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
            .Select((t, idx) => new EndpointDetail(Convert.ToUInt64(enumValues[idx]), t))
            .ToList();
        EndpointDetails = [.. details];
    }

    public string ClaimType { get; }
    public EndpointDetail[] EndpointDetails { get; }
}
