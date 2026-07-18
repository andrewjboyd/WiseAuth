using Microsoft.AspNetCore.Identity;

namespace WiseAuth.Sample.Data;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
