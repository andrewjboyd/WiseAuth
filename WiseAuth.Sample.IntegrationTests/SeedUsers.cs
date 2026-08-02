namespace WiseAuth.Sample.IntegrationTests;

// Credentials for the three accounts DbSeeder.SeedAsync creates on every fresh database.
internal static class SeedUsers
{
    public const string AdminUserName = "Admin";
    public const string AdminPassword = "Admin123!";

    public const string ViewerUserName = "Viewer";
    public const string ViewerPassword = "Viewer123!";

    public const string AuditorUserName = "Auditor";
    public const string AuditorPassword = "Auditor123!";
}
