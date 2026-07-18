namespace WiseAuth.Sample.Roles;

public record RoleSummary(string Id, string Name, Dictionary<string, ulong> Permissions);

public record CreateRoleRequest(string Name, Dictionary<string, ulong>? Permissions);

public record UpdateRolePermissionsRequest(Dictionary<string, ulong>? Permissions);
