namespace WiseAuth.Sample.Modules.Roles;

[ClaimType("roles")]
public enum RolePermissions
{
    View = 1,
    Manage = 2,
}
