# WiseAuth.Sample

An end-to-end demo of [WiseAuth](../README.md): an ASP.NET Core API + embedded React app backed by SQLite (EF Core) and ASP.NET Identity, with a small RBAC-style Security area (Users, Roles, and a read-only Access Controls view) so you can see the library actually allow and deny requests across a realistic permission model, not just a flat claims list.

## How auth works here (BFF pattern)

`POST /api/auth/login` checks the password via `SignInManager`, signs the user in, and sets the resulting session as an **HttpOnly, Secure, SameSite=Strict cookie** — nothing ever reaches browser JavaScript or `localStorage`. The React app only ever sees `{ displayName, permissions }`, fetched from `GET /api/auth/me` on load and from the login response. `POST /api/auth/logout` clears the cookie.

This is same-origin end to end (the SPA is served by this same app), so the cookie is sent automatically on every request and `SameSite=Strict` keeps it from ever being attached to a cross-site request — the practical CSRF mitigation for this setup. A production deployment would layer ASP.NET Core's antiforgery middleware on top.

## Permissions

```csharp
[ClaimType("products")]
public enum ProductPermissions
{
    Read = 1,
    Write = 2,
    Delete = 4,
    Export = 8,
}

[ClaimType("users")]
public enum UserPermissions
{
    View = 1,
    Manage = 2,
}

[ClaimType("roles")]
public enum RolePermissions
{
    View = 1,
    Manage = 2,
}
```

Registered with `AddWiseAuth<ProductPermissions>()` / `AddWiseAuth<UserPermissions>()` / `AddWiseAuth<RolePermissions>()` and enforced per-route via `.EndpointId(...)` in `Products/ProductEndpoints.cs` / `Users/UserEndpoints.cs` / `Roles/RoleEndpoints.cs`. Three independently-registered enums compose on the same principal — the Security area's Users and Roles tabs are each gated by their own enum (assigning a role *to* a user is a `UserPermissions.Manage` action; editing a role's *own* permission bitmask is a `RolePermissions.Manage` action) — and every permission editor in the UI is driven entirely by `GET /api/auth/permissions-schema`, so it renders a checkbox group per registered enum without hardcoding any claim type.

## How permissions combine

Roles are native ASP.NET Core Identity roles (`IdentityRole`), and a role's permissions are just role claims (`RoleManager.AddClaimAsync`) — the same claim-type/value shape as a user's own claims, scoped to the role instead. A user's **effective** permission for a claim type is `(OR of all their roles' bits) OR (their own claim bits)`: a personal claim can only add bits on top of what a role grants, never take one away.

ASP.NET Core Identity's default claims-principal factory would otherwise put a role's claims and a user's own claims on the principal as separate, non-deduplicated `Claim` objects — and `WiseAuthorizationHandler<T>` only reads the *first* matching claim, not all of them. `Security/MergingUserClaimsPrincipalFactory.cs` overrides the default factory to OR every claim of a given type together into one, so the merge happens once and everything downstream (real enforcement, `/api/auth/me`, the Users list) sees a single, correct effective value.

One consequence worth knowing: Identity's `SecurityStampValidator` re-runs this factory on a roughly 30-minute interval for already-signed-in sessions, so a role or claim change made to *someone else's* account takes effect automatically within that window, not only the next time they log in.

## Seeded accounts

| Username  | Password      | Role(s)   | Personal claims       | Effective `products` | Can do                                    |
|-----------|---------------|-----------|------------------------|-----------------------|--------------------------------------------|
| `Admin`   | `Admin123!`   | `Admins`  | none                    | `15` (all)             | Everything — access comes entirely from the role |
| `Viewer`  | `Viewer123!`  | `Viewers` | none                    | `1` (Read)              | Read only                                   |
| `Auditor` | `Auditor123!` | `Viewers` | `products` = `8` (Export) | `9` (Read \| Export)  | Read (from role) + Export (from personal claim) |

`Admins` grants all bits across `products`/`users`/`roles`; `Viewers` grants `products` = Read only. `Auditor` is the account that demonstrates the merge: it holds no more than `Viewers` at the role level, but its own `products` claim adds Export on top — effective access is the union, without granting Export to every Viewer.

Log in as `Viewer` and the Security nav link disappears entirely (no `View` permission on `users` or `roles`); calling those routes directly still gets a `403` from `WiseAuthorizationHandler` server-side, which is the actual point of the demo — the UI hiding controls is cosmetic, the server is what's enforcing it.

**These are dev-only credentials — never hardcode credentials like this in a real application.**

## Running it

Prerequisites: .NET 10 SDK, Node 20+.

```bash
cd WiseAuth.Sample
dotnet run
```

That's it — an MSBuild target (`BuildClientApp` in `WiseAuth.Sample.csproj`) runs `npm install`/`npm run build` automatically before `Build` so there's no separate npm step. It's guarded by `Condition="'$(CI)' != 'true'"` and skipped entirely on CI (GitHub Actions, and most CI systems, set the `CI` env var), so it can't affect the solution-wide `dotnet build`/`dotnet test` the pipeline runs — the client app is only ever built for a local `dotnet run`/`dotnet build`. It also uses MSBuild's `Inputs`/`Outputs`, so unchanged rebuilds skip the npm step entirely instead of re-running it every time.

Open the printed `https://localhost:...` URL and log in as `Admin`, `Viewer`, or `Auditor`.

A SQLite database (`wiseauth-sample.db`) is created and seeded automatically on first run via `Database.EnsureCreated()` — there are no EF Core migrations in this sample, since the point is demonstrating WiseAuth/Identity/React wiring rather than the migration workflow.

### Frontend dev loop

For hot-reload while working on the React app, run the API (`dotnet run`) and the Vite dev server side by side:

```bash
cd WiseAuth.Sample/ClientApp
npm run dev
```

`vite.config.ts` proxies `/api` to `http://localhost:5100`, so cookies and API calls work the same way as the production build.

## Routing: SPA vs API

All backend endpoints live under `/api/*`; everything else falls back to serving the React app (`app.MapFallbackToFile("index.html")` in `Program.cs`). That's what makes a hard refresh on a client-side route like `/products` work — without the `/api` prefix, that path would collide with the `GET /products` API route and the server would return JSON instead of the app shell. An unmatched `/api/*` path still gets a real `404` (see `app.MapFallback("/api/{**path}", ...)` in `Program.cs`) rather than silently returning the SPA shell; genuinely bad client-side URLs are handled by React Router's catch-all route (`NotFoundPage`), since the server can't tell a stale link from a valid one once everything non-API falls back to `index.html`.

## Endpoints

| Route | Permission | Notes |
|---|---|---|
| `POST /api/auth/login` | — | Sets the session cookie; returns `{ displayName, permissions }` (effective, post-merge) |
| `GET /api/auth/me` | authenticated | Rehydrates `{ displayName, permissions }` from the live, already-merged session |
| `POST /api/auth/logout` | — | Clears the cookie |
| `GET /api/products` | `Read` | |
| `GET /api/products/export` | `Export` | Returns CSV |
| `POST /api/products` | `Write` | |
| `PUT /api/products/{id}` | `Write` | |
| `DELETE /api/products/{id}` | `Delete` | |
| `GET /api/users` | `View` (users) | Lists users with their **effective** permissions (role ∪ claims) across every registered claim type |
| `GET /api/users/{id}` | `View` (users) | Username/display name/email for the detail page header |
| `PUT /api/users/{id}/profile` | `Manage` (users) | Updates username/display name/email; `400` with Identity's validation errors (e.g. duplicate username). No self-edit block — unlike claims/roles, this isn't a privilege-escalation vector |
| `POST /api/users` | `Manage` (users) | Creates a user; `400` with Identity's validation errors on failure (e.g. weak password) |
| `GET /api/users/{id}/claims` | `View` (users) | The user's own raw claims (not merged with roles) |
| `PUT /api/users/{id}/claims` | `Manage` (users) | Replaces a user's own claims; `400` if editing your own |
| `GET /api/users/{id}/roles` | `View` (users) | `{ memberRoleNames, allRoleNames }` — the assignable-role catalog lives here so assignment never requires `RolePermissions` |
| `PUT /api/users/{id}/roles` | `Manage` (users) | Replaces a user's role memberships; `400` if editing your own |
| `GET /api/roles` | `View` (roles) | Lists roles with their own permission claims |
| `POST /api/roles` | `Manage` (roles) | Creates a role; `400` with Identity's validation errors on failure (e.g. duplicate name) |
| `PUT /api/roles/{id}/permissions` | `Manage` (roles) | Replaces a role's permission claims |
| `GET /api/auth/permissions-schema` | — | `IWiseAuthService.GetDetails()` — runtime discovery of registered permission enums, consumed by every permission editor and the read-only Access Controls tab |

## Known warning

`dotnet build` prints a `NU1903` advisory for `SQLitePCLRaw.lib.e_sqlite3` — a transitive dependency of `Microsoft.EntityFrameworkCore.Sqlite` upstream, not something this sample controls. Harmless for local demo use.
