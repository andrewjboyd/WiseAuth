# WiseAuth

A lightweight ASP.NET Core authorization library that uses power-of-two bit-flag enums and bitwise claim checking to enforce per-endpoint permissions.

## How it works

1. You define a permission enum where every value is a power of two (1, 2, 4, 8, 16, …).
2. At startup, `AddWiseAuth<T>()` validates the enum and registers an `IAuthorizationHandler` for it.
3. Each route is decorated with `.EndpointId<T>(value)`, which attaches an `IAuthorizationRequirement` containing that value.
4. On each request, the handler reads the user's claim, interprets it as a bitmask, and checks `(claimValue & endpointId) != 0`.

Because each permission occupies its own bit, any combination of permissions can be encoded as a single integer (the OR/sum of the selected values) and individual permissions can be checked with a single AND operation without colliding with any other combination.

## Getting started

### 1. Define a permission enum

Values **must** form a power-of-two sequence starting at 1 with no gaps (1, 2, 4, 8, 16, …). The order of members in the enum does not matter; the validator sorts them before checking.

```csharp
public enum ProductPermissions
{
    Read   = 1,
    Write  = 2,
    Delete = 4,
    Export = 8,
}
```

If the enum is nested inside a controller class, the claim type is derived automatically by stripping the `Controller` suffix. For a fully custom name, apply `[ClaimType]`:

```csharp
public class ProductController
{
    // Claim type → "ProductPermissions" (Controller suffix stripped)
    public enum Permissions
    {
        Read   = 1,
        Write  = 2,
        Delete = 4,
    }
}

[ClaimType("products")]
public enum ProductPermissions { ... }  // Claim type → "products"
```

### 2. Register with the DI container

Call `AddWiseAuth<T>()` once per enum in `Program.cs`. An exception is thrown at startup if the enum values are not a valid power-of-two sequence.

```csharp
builder.Services.AddAuthentication(...);
builder.Services.AddAuthorization();

builder.Services.AddWiseAuth<ProductController.Permissions>();
builder.Services.AddWiseAuth<OrderPermissions>();
```

Multiple calls are safe — `IWiseAuthService` is registered as a singleton only once via `TryAddSingleton`.

### 3. Protect routes

#### Minimal APIs

Use the `.EndpointId<T>()` extension method on any `RouteHandlerBuilder`. This attaches the requirement metadata and calls `RequireAuthorization()` automatically.

```csharp
app.MapGet("/products", GetProducts)
   .EndpointId(ProductController.Permissions.Read);

app.MapPost("/products", CreateProduct)
   .EndpointId(ProductController.Permissions.Write);

app.MapDelete("/products/{id}", DeleteProduct)
   .EndpointId(ProductController.Permissions.Delete);
```

#### Attribute-routed controllers

For `[ApiController]`-based controllers, apply `[EndpointId<T>(value)]` to an action (or the whole controller). It derives from `AuthorizeAttribute` and attaches the same requirement the minimal-API extension does, so it's enforced by the same registered `IAuthorizationHandler<T>` — no other setup differs.

```csharp
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    [EndpointId<ProductController.Permissions>(ProductController.Permissions.Read)]
    public IActionResult GetProducts() => Ok(...);

    [HttpPost]
    [EndpointId<ProductController.Permissions>(ProductController.Permissions.Write)]
    public IActionResult CreateProduct(CreateProductRequest request) => Created(...);
}
```

### 4. Issue claims

When issuing tokens, store the user's permissions as the sum (equivalently, the bitwise OR) of their allowed power-of-two values for each claim type:

```csharp
// User can Read (1) and Export (8) → claim value = 1 + 8 = 9
new Claim("ProductPermissions", "9")
```

The handler checks `(claimValue & endpointId) != 0`. This works correctly because each permission occupies a distinct, non-overlapping bit: since every value is a power of two, no combination of permissions can ever produce the same bit pattern as a different permission or combination.

## Backing types and permission limits

C# enums default to `int` as their backing type, which supports up to **31 permissions** (the sign bit can't be used for a flag). You can explicitly specify a larger backing type to increase this limit:

```csharp
public enum ProductPermissions : ulong  // supports up to 64 permissions
{
    Read   = 1,
    Write  = 2,
    Delete = 4,
    Export = 8,
}
```

The authorization handler always compares values as `ulong` internally, so upgrading the backing type requires no other changes.

| Backing type | Max value | Max permissions |
|---|---|---|
| `sbyte` | 127 | 7 |
| `byte` | 255 | 8 |
| `short` | 32,767 | 15 |
| `ushort` | 65,535 | 16 |
| **`int` (default)** | 2,147,483,647 | **31** |
| `uint` | 4,294,967,295 | 32 |
| `long` | 9,223,372,036,854,775,807 | 63 |
| `ulong` | 18,446,744,073,709,551,615 | 64 |

## Claim type naming conventions

When no `[ClaimType]` attribute is present, the claim type is derived from the enum's fully-qualified name:

| Enum location | Example | Claim type |
|---|---|---|
| Top-level | `StandaloneEnum` | `StandaloneEnum` |
| Nested in a non-controller class | `Widget+Permissions` | `WidgetPermissions` |
| Nested in a controller class | `ProductController+Permissions` | `ProductPermissions` |
| Custom attribute | `[ClaimType("products")]` | `products` |

## Discovering permissions at runtime

`IWiseAuthService` exposes all registered enum details, useful for generating documentation or admin UIs:

```csharp
app.MapGet("/auth/permissions", (IWiseAuthService wiseAuth) =>
{
    return wiseAuth.GetDetails();
});
// Returns: { "ProductPermissions": [{ "id": 1, "name": "Read" }, ...] }
```

## Enum validation rules

`AddWiseAuth<T>()` enforces these constraints at startup — an invalid enum throws immediately rather than failing silently at runtime:

- All values must be greater than 0.
- The lowest value must be 1.
- No duplicate values.
- Values must form a complete power-of-two sequence with no gaps (e.g., skipping 4 in the sequence 1, 2, 8 is invalid).

## Sample project

[`WiseAuth.Sample`](WiseAuth.Sample/README.md) is a runnable end-to-end demo: an ASP.NET Core API + embedded React app, SQLite via EF Core, ASP.NET Identity, and three seeded users (`Admin`, full access; `Viewer`, read-only; `Auditor`, read-only plus one extra permission) so you can see WiseAuth both allow and deny requests. It includes a small RBAC-style Security area — Users, Roles (native ASP.NET Identity roles), and a read-only Access Controls view — with three independently-registered permission enums composing on one principal, and a custom `IUserClaimsPrincipalFactory` that merges a user's role-granted and individually-granted permissions into the single effective claim WiseAuth actually enforces.

## Requirements

- .NET 10.0
- `Microsoft.AspNetCore.App`

## License

GPL-3.0-only
