namespace WiseAuth;

internal sealed class WiseAuthService(IEnumerable<IWiseAuthDetails> endpointDetails) : IWiseAuthService
{
    public Dictionary<string, EndpointDetail[]> GetDetails()
    {
        return endpointDetails.ToDictionary(d => d.ClaimType, d => d.EndpointDetails);
    }
}