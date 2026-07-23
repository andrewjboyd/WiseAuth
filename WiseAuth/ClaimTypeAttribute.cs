namespace WiseAuth;

[AttributeUsage(AttributeTargets.Enum)]
public class ClaimTypeAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
