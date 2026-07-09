namespace WiseAuth;

public interface IWiseAuthService
{
    Dictionary<string, EndpointDetail[]> GetDetails();
}