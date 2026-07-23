namespace WiseAuth;

public interface IWiseAuthDetails
{
    string ClaimType { get; }
    EndpointDetail[] EndpointDetails { get; }
}
