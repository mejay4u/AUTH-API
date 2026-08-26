# Member Auth API

A production-style **.NET 8** authentication API for the Member Portal, built with **Clean Architecture**
(following [jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture)) and
patterns popularised by **Milan Jovanović** (Result pattern, thin endpoints, CQRS via MediatR,
options validation, global exception handling).

A member logs in with username + password; on success the API returns a **short-lived RS256 JWT**
(carrying the member's **LOBs** and **Plan IDs**) plus a **rotating refresh token**. Other
microservices validate that JWT using the public key published at the **JWKS** endpoint.

---

## Solution layout (Clean Architecture)

```
src/
  Domain/          Entities (Member, Lob, Plan, RefreshToken), Result/Error primitives. No dependencies.
  Application/     Use cases (Login, Refresh) as MediatR CQRS, interfaces, FluentValidation, behaviors.
  Infrastructure/  EF Core DbContext, password verifier, RSA JWT/token service, DI. Depends on Application.
  Api/             Minimal-API endpoints, JWT auth, rate limiting, security headers, Swagger, JWKS.
```

Dependencies point **inward**: `Api → Infrastructure → Application → Domain`. The Application layer
talks to the database only through `IApplicationDbContext`, so swapping the data source touches one line.

---

## Running it

> Prerequisite: the **.NET 8 SDK**. This repo pins it via `global.json`.

```bash
dotnet run --project src/Api/AuthApi.Api.csproj
```

The API starts in **Development** with a **mocked in-memory database seeded with demo data**, and Swagger
at `/swagger`. No real database is required to try it.

### Seeded demo members (Development only)

| Username | Password     | LOBs            | Plan IDs     |
|----------|--------------|-----------------|--------------|
| `jdoe`   | `P@ssw0rd!`  | DENTAL, VISION  | 1001, 2001   |
| `asmith` | `Secret123!` | MEDICAL         | 3001         |

### Endpoints

| Method | Route                                | Auth      | Purpose                                            |
|--------|--------------------------------------|-----------|----------------------------------------------------|
| POST   | `/api/v1/auth/login`                 | anonymous | Validate credentials → access + refresh tokens     |
| POST   | `/api/v1/auth/refresh`               | anonymous | Rotate refresh token → new access token            |
| GET    | `/api/v1/members/me`                 | **JWT**   | Test endpoint: returns member details from claims  |
| GET    | `/.well-known/jwks.json`             | anonymous | Public keys for downstream services to validate    |
| GET    | `/.well-known/openid-configuration`  | anonymous | Minimal OIDC discovery document                    |
| GET    | `/health`                            | anonymous | Health probe                                       |

See [`requests.http`](requests.http) for ready-to-run calls.

---

## Two modes: local issuance and BFA pass-through

The API ships one image that can run either way, selected by `Auth:Mode`:

| `Auth:Mode` | What the process does | Needs |
|---|---|---|
| `Local` *(default)* | Authenticates against the member DB and issues its own RS256 tokens | Line of sight to the LOB databases |
| `PassThrough` | Acts as a **BFA**: relays the call to the on-prem **ARTS** auth service via Apigee Internal and returns ARTS's answer verbatim | A route to Apigee Internal |

`PassThrough` exists because the API runs in **ARO**, which has no route to the on-prem member
databases — while the on-prem ARTS .NET auth service already has one.

```
Member Portal → Apigee External → BFA (ARO) → Apigee Internal → ARTS (on-prem) → member DB
```

The routes are identical in both modes, so the Member Portal cannot tell the difference. What
changes is who decides: in `PassThrough` the BFA holds **no signing key, no DB credential and no
password logic** — it forwards the request body byte-for-byte and replays ARTS's status, body and
allow-listed headers. The only responses it authors itself are `400` (malformed body), `503`
(ARTS unreachable) and `504` (ARTS too slow).

```bash
# run as the BFA
Auth__Mode=PassThrough \
ApigeeInternal__BaseAddress=https://internal-apigee.corp/arts/v1/ \
ApigeeInternal__ApiKey=... \
dotnet run --project src/Api/AuthApi.Api.csproj
```

**→ [`docs/PassThrough-Architecture.md`](docs/PassThrough-Architecture.md)** covers the design in
full: why the relay is opaque rather than typed, the Result semantics at the gateway boundary, the
deliberately conservative retry policy, header handling, the cut-over plan, and one known gap — the
per-IP rate limiter partitions on Apigee's address rather than the caller's until the Apigee egress
ranges are configured as trusted proxies.

---

## Security features

- **Asymmetric RS256 signing.** The Auth API holds the private key; every other service validates with
  the **public key only** (via JWKS). No shared secret can mint tokens. Key is 3072-bit RSA.
- **Short-lived access tokens** (default 15 min) + **rotating refresh tokens**. Refresh tokens are stored
  only as a **SHA-256 hash**; the raw value is shown once. **Reuse of a rotated token revokes the chain.**
- **Brute-force protection:** per-IP **rate limiting** on auth endpoints + **account lockout** after N
  failed attempts.
- **No user enumeration:** unknown user / wrong password / inactive account all return the same generic
  401, and the unknown-user path still performs a hash verification to equalise response timing.
- **Constant-time** password comparison (`CryptographicOperations.FixedTimeEquals`).
- **Options validated on startup** (fail-fast) and the signing key is constructed before serving traffic.
- **Security headers** (nosniff, DENY framing, no-referrer, permissions-policy), **HSTS** (non-dev),
  **HTTPS redirection**, locked-down **CORS**.
- **RFC 7807 ProblemDetails** for all errors via a global `IExceptionHandler`; no internal details leak.
- Tokens use `typ: at+jwt` and original claim names (`sub`, `lob`, `plan`) — not remapped to legacy XML URIs.

### JWT claims issued

`sub`, `jti`, `unique_name`, `email`, `given_name`, `family_name`, `iss`, `aud`, `iat`, `nbf`, `exp`,
and one `lob` claim per line of business + one `plan` claim per plan ID.

---

## Switching from the mock to the real existing database

Everything is wired through `IApplicationDbContext` + EF Core, so moving to the real member DB is config-only:

1. Set the provider and connection string (e.g. in `appsettings.Production.json` or user-secrets):

   ```json
   {
     "Database": { "Provider": "SqlServer", "SeedMockData": false },
     "ConnectionStrings": { "AuthDb": "Server=...;Database=...;Trusted_Connection=True;Encrypt=True" }
   }
   ```

2. Align the EF mappings in `src/Infrastructure/Persistence/Configurations/*` to the real table/column names.

3. **Match the password scheme.** Set the `PasswordHashing` section to exactly how the existing DB hashes
   passwords (algorithm, salt placement, encoding, iterations) — `SaltedHashPasswordHasher` is built to be
   configured, not rewritten:

   ```json
   "PasswordHashing": {
     "Algorithm": "Sha512",
     "SaltPlacement": "Prefix",       // H(salt || password) vs "Suffix" = H(password || salt)
     "HashEncoding": "Base64",        // or "Hex"
     "SaltEncoding": "Base64",
     "Iterations": 1
   }
   ```

No application or domain code changes are required.

---

## Moving to .NET 10

The target framework is centralized in [`Directory.Build.props`](Directory.Build.props). To upgrade:

1. Change `<TargetFramework>net8.0</TargetFramework>` → `net10.0`.
2. Point `global.json` at a .NET 10 SDK.
3. Remove `<RollForward>Major</RollForward>` from `src/Api/AuthApi.Api.csproj` (only needed to run a
   net8.0 build on a newer installed runtime).

---

## Production checklist

- [ ] Provide the RSA private key from a secret store / Key Vault (`Jwt:PrivateKeyPem` or `Jwt:PrivateKeyPath`),
      never commit `keys/`.
- [ ] Set `Database:Provider=SqlServer`, `SeedMockData=false`, and a real connection string.
- [ ] Confirm the `PasswordHashing` options match the existing DB exactly.
- [ ] Configure real `Cors:AllowedOrigins` and serve over HTTPS.
- [ ] Consider key rotation (publish multiple keys in JWKS by `kid`).
