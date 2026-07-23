namespace WiseAuth.Sample.Modules.Users;

[ClaimType("users")]
public enum UserPermissions
{
    View = 1,
    Manage = 2,
}
