# Registration / Identity Profile Service

A **standalone .NET 8** service (Clean Architecture) that is the **profile / identity system of record**
for the member portal. **Descope owns authentication, passwords, hashing, and the registration/email
wizard** — this service stores **no password material**. It exists to:

1. **Receive users Descope pushes** to us (registration / profile updates) — the sync webhook.
2. **JIT-migrate legacy users** on first login — Descope's verify hook calls us; we verify against the
   legacy system and create the profile so Descope takes over the password. Current users aren't disturbed.

```
registration/
  src/
    Domain/          Result/Error, User (passwordless) + UserOrigin, MigrationAudit, RegistrationErrors.
    Application/     CQRS use cases (SyncDescopeUser, VerifyLegacyLogin), FluentValidation, interfaces, models.
    Infrastructure/  EF Core DbContext (database-first), EF repositories, legacy verifier (HTTP + dev mock).
    Api/             Minimal-API endpoints, Descope HMAC signature middleware, ProblemDetails, rate limiting, Swagger.
  tests/
    Registration.UnitTests/  xUnit tests (handlers, validators, EF repository).
```

Dependencies point inward: `Api → Infrastructure → Application → Domain`.

## Endpoints (Descope calls these, machine-to-machine)

| Method | Route                                   | Purpose                                                             |
|--------|-----------------------------------------|---------------------------------------------------------------------|
| POST   | `/api/v1/registration/descope/users`    | Idempotent upsert of a user Descope pushed (registration/update)    |
| POST   | `/api/v1/registration/descope/verify`   | JIT verify a legacy member on first login → profile (Descope hook)  |
| GET    | `/health`                               | Health probe                                                        |

All `/descope/*` calls are **HMAC-signature verified** (`DescopeSignatureMiddleware`) and gated by the
**`Registration` feature flag**. Signature failure → 401 before the handler runs.

## JIT (lazy) migration flow
1. A legacy user logs in through Descope for the first time.
2. Descope calls `POST /descope/verify` with the credentials (HMAC-signed).
3. We look up our DB by email → **already known** → return the profile. Otherwise call the **legacy verify
   API** (`ILegacyMemberVerifier`), which verifies the salted **SHA-256/512** password *inside the legacy
   system* and returns the profile.
4. On success we create a `User` (`Origin = Migrated`, `LegacyMemberId`, `MigratedUtc`) and write a
   `MigrationAudit` row; Descope stores the password thereafter. On failure → 401 + audit. **We never
   hash or store a password.**

## Running it (Development)
```bash
dotnet run --project registration/src/Api/Registration.Api.csproj
```
Development uses the **EF Core InMemory** provider, **skips signature verification** (`Descope:Enabled=false`),
and uses the **mock legacy verifier** (accepts `legacy.user@example.com` / `Legacy#123`). So you can call
`/descope/verify` and `/descope/users` straight from Swagger (`/swagger`). See [`requests.http`](requests.http).

## Database (database-first, `registration` schema)
- `registration.Users` — profile system of record; **no password columns**. Email/username unique;
  `DescopeUserId` uniquely indexed when present; `Origin` = `Registration` | `Migrated`; `LegacyMemberId`,
  `MigratedUtc` for provenance.
- `registration.MigrationAudit` — append-only JIT audit.

DDL: [`scripts/create-registration-user-table.sql`](scripts/create-registration-user-table.sql). EF maps
to it (no app migrations).

## Configuration
- `Database:Provider` + `ConnectionStrings:RegistrationDb` — our own DB.
- `Descope:*` — `SigningSecret` (Key Vault), timestamp tolerance, header names, `Enabled`.
- `LegacyVerifyApi:*` — `BaseUrl`, `ApiKey`, `UseMock`.
- `FeatureManagement:Registration`, `RateLimiting`, `Cors`.

## Going to production
1. Run the DDL against the registration DB; set `Database:Provider=SqlServer` + `ConnectionStrings:RegistrationDb`.
2. Set the real `Descope:SigningSecret` and align the signature header/payload format with your Descope
   webhook config; leave `Descope:Enabled=true`. Consider IP-allowlisting Descope's egress ranges.
3. Point `LegacyVerifyApi:BaseUrl`/`ApiKey` at the legacy verify API and set `UseMock=false`.

## Ownership at a glance
- **Descope:** authN, sessions, passwords/hashing, the registration + email wizard, CSR impersonation.
- **This service:** profile system of record, Descope→us sync, JIT migration + audit.
- **Legacy system:** verifies legacy credentials (salted SHA-256/512) during migration only.

## Related
The **member-experience / authorization** design (dashboard, view switching, LOB/plan/role views, CSR,
consent, Facets) is a separate concern — see [`docs/member-experience-architecture.md`](docs/member-experience-architecture.md).
