using WiseAuth;

namespace WiseAuth.Sample.Products;

[ClaimType("products")]
public enum ProductPermissions
{
    Read = 1,
    Write = 2,
    Delete = 4,
    Export = 8,
}
