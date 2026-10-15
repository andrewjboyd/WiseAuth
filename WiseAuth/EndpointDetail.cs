namespace WiseAuth;

public class EndpointDetail(ulong id, string name, string? title, string? description)
{
    public EndpointDetail(ulong id, string name) : this(id, name, null, null)
    {
    }

    public ulong Id { get; } = id;
    public string Name { get; } = name;
    public string Title { get; } = string.IsNullOrWhiteSpace(title) ? name : title;
    public string Description { get; } = description ?? string.Empty;
}
