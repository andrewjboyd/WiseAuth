namespace WiseAuth.Sample.Modules.Roles;

[ClaimType("roles")]
[Flags]
public enum RolePermissions
{
    View = 1,
    Manage = 2,
}
