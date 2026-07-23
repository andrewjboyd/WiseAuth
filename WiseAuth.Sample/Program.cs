using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WiseAuth;
using WiseAuth.Sample.Data;
using WiseAuth.Sample.Modules.Auth;
using WiseAuth.Sample.Modules.Products;
using WiseAuth.Sample.Modules.Roles;
using WiseAuth.Sample.Modules.Users;
using WiseAuth.Sample.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<MergingUserClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.Name = "WiseAuth";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.LoginPath = "/login";
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    // Without this, a Forbid() on a GET request 302-redirects to /Account/AccessDenied
    // (no such route), which falls through to the SPA's index.html fallback - fetch()
    // follows the redirect and silently returns 200 instead of a real 403.
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>();

builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddWiseAuth<ProductPermissions>();
builder.Services.AddWiseAuth<UserPermissions>();
builder.Services.AddWiseAuth<RolePermissions>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
// Products uses attribute-routed ApiController + [EndpointId<T>] instead of minimal API's
// .EndpointId() extension, to demonstrate WiseAuth's controller-based support.
app.MapControllers();
app.MapUserEndpoints();
app.MapRoleEndpoints();

// Runtime discovery of registered permission enums, as described in the WiseAuth README.
app.MapGet("/api/auth/permissions-schema", (IWiseAuthService wiseAuth) => wiseAuth.GetDetails());

// API routes live under /api, so any other path is a client-side React Router route (or a stale
// deep link) - hard-refreshing /products should re-render the SPA shell, not hit the API. An
// unmatched /api/* path still gets a real 404 instead of silently returning the SPA shell; the
// React app owns 404 handling for its own client-side routes.
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();
