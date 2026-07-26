namespace WiseAuth.Sample.Modules.Users;

public record UserSummary(string Id, string UserName, string DisplayName, string Email, Dictionary<string, ulong> EffectivePermissions);

public record UserDetail(string Id, string UserName, string DisplayName, string Email);

public record CreateUserRequest(string UserName, string DisplayName, string Email, string Password, Dictionary<string, ulong>? Permissions);

public record UserRolesResponse(string[] MemberRoleNames, string[] AllRoleNames);

// The following request DTOs use mutable properties rather than positional records: FastEndpoints
// populates them from two sources - the {id} route segment and the JSON body - and binds by
// property name into a plain settable property rather than through a constructor.

public sealed class UserIdRequest
{
    public string Id { get; set; } = string.Empty;
}

public sealed class UpdateUserProfileRequest
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class UpdateUserClaimsRequest
{
    public string Id { get; set; } = string.Empty;
    public Dictionary<string, ulong>? Permissions { get; set; }
}

public sealed class UpdateUserRolesRequest
{
    public string Id { get; set; } = string.Empty;
    public string[] RoleNames { get; set; } = [];
}
