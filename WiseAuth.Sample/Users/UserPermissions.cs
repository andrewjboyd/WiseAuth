using WiseAuth;

namespace WiseAuth.Sample.Users;

[ClaimType("users")]
public enum UserPermissions
{
    View = 1,
    Manage = 2,
}
