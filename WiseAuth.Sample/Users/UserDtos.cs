namespace WiseAuth.Sample.Users;

public record UserSummary(string Id, string UserName, string DisplayName, string Email, Dictionary<string, ulong> EffectivePermissions);

public record UserDetail(string Id, string UserName, string DisplayName, string Email);

public record CreateUserRequest(string UserName, string DisplayName, string Email, string Password, Dictionary<string, ulong>? Permissions);

public record UpdateUserClaimsRequest(Dictionary<string, ulong>? Permissions);

public record UpdateUserProfileRequest(string UserName, string DisplayName, string Email);

public record UpdateUserRolesRequest(string[] RoleNames);

public record UserRolesResponse(string[] MemberRoleNames, string[] AllRoleNames);
