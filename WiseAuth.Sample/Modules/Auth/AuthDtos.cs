namespace WiseAuth.Sample.Modules.Auth;

public record LoginRequest(string UserName, string Password);

public record AuthResponse(string Id, string DisplayName, Dictionary<string, ulong> Permissions);
