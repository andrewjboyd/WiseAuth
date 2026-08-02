namespace WiseAuth.Sample.Modules.Users;

[ClaimType("users")]
[Flags]
public enum UserPermissions
{
    View = 1,
    Manage = 2,
}
