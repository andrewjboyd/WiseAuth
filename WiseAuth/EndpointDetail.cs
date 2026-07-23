namespace WiseAuth;

public class EndpointDetail(ulong id, string name)
{
    public ulong Id { get; } = id;
    public string Name { get; } = name;
}
