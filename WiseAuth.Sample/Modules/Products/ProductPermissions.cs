namespace WiseAuth.Sample.Modules.Products;

[ClaimType("products")]
[Flags]
public enum ProductPermissions
{
    Read = 1,
    Write = 2,
    Delete = 4,
    Export = 8,
}
