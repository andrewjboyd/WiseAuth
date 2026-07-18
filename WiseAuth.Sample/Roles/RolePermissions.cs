using WiseAuth;

namespace WiseAuth.Sample.Roles;

[ClaimType("roles")]
public enum RolePermissions
{
    View = 1,
    Manage = 2,
}
