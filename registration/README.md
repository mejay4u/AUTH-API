# Member Registration API

A **new, standalone .NET 8** service (Clean Architecture) that backs the portal sign-up flow:
**email OTP verification** and **user account creation**. It is intentionally separate from the
existing Auth/login service — the **only** thing they share is the **existing user database**.

```
registration/
  src/
    Domain/          Result/Error primitives, RegistrationErrors. No dependencies.
    Application/     CQRS use cases (SendOtp, VerifyOtp, CreateAccount), FluentValidation, interfaces, options.
    Infrastructure/  EF Core DbContext (database-first), PBKDF2 hasher, cache-backed OTP service, SMTP/dev email senders.
    Api/             Minimal-API endpoints, ProblemDetails, rate limiting, feature flag, Swagger.
  tests/
    Registration.UnitTests/  xUnit tests (password policy, OTP service, PBKDF2 hasher).
```

Dependencies point inward: `Api → Infrastructure → Application → Domain`.

## Endpoints

| Method | Route                              | Purpose                                                        |
|--------|------------------------------------|----------------------------------------------------------------|
| POST   | `/api/v1/registration/otp/send`    | Send a verification code to the email                          |
| POST   | `/api/v1/registration/otp/resend`  | Resend the code (60s cooldown, per-email cap enforced)         |
| POST   | `/api/v1/registration/otp/verify`  | Verify the emailed code                                        |
| POST   | `/api/v1/registration/account`     | Create the user (email = username) after verification          |
| GET    | `/health`                          | Health probe                                                   |

The whole surface is gated by the **`Registration` feature flag** (`FeatureManagement:Registration`);
when disabled every endpoint returns 404.

## Running it (Development)

```bash
dotnet run --project registration/src/Api/Registration.Api.csproj
```

In **Development** it uses the **EF Core InMemory provider** and a **logging email sender** (the OTP is
written to the console instead of emailed), so the full flow runs with no database or SMTP server.
Swagger is served at `/swagger`.

Typical flow: `POST /otp/send` → grab the code from the console log → `POST /otp/verify` →
`POST /account`. See [`requests.http`](requests.http).

## Key behaviours

- **Email is the username / User ID.**
- **Full personal information persisted** from the registration screen: first name, last name, date
  of birth, ZIP code, email, and optional contact number — all server-validated on account creation.
- **Age gate:** applicants must be **16 or older** (validated from date of birth).
- **Own database, database-first EF Core:** `RegistrationDbContext` maps the `User` entity to the
  `registration.Users` table whose schema is authored in SQL (`scripts/create-registration-user-table.sql`)
  — EF maps to it and does **not** own migrations. A **unique index on email/username** is the real
  duplicate guard.
- **Best-practice password hashing:** PBKDF2 (HMAC-SHA256, random per-password salt), behind
  `IPasswordHasher` so the scheme (`PasswordHashing:Scheme`) can be swapped (Argon2id/BCrypt) later.
- **Server-side validation is the source of truth:** FluentValidation → RFC 7807
  `ValidationProblemDetails` (400) listing exactly what is required.
- **Duplicate email**, checked early and late: at **OTP send** the flow is refused for an already-
  registered email in an **enumeration-safe** way (same generic response, no code issued), and again at
  **account creation** → 409 Conflict. A **unique DB index** on email/username is the final guard.
- **OTP:** 6-digit code, stored only as a SHA-256 hash, with expiry, a wrong-guess attempt cap, a
  60s resend cooldown, and a configurable per-email request cap (default 10). State lives in a
  cache — **no new tables**.
- **Configurable everywhere** via options (`PasswordPolicy`, `Otp`, `PasswordHashing`, `Smtp`,
  `RateLimiting`, `Cors`), validated on startup.

## Going to production

1. Create the schema by running [`scripts/create-registration-user-table.sql`](scripts/create-registration-user-table.sql)
   against the registration database (database-first — EF maps to it, it does not create it). Then set
   `Database:Provider=SqlServer` and `ConnectionStrings:RegistrationDb`. Because this is registration's
   **own** database, whatever authenticates these users reads from **this** DB.
2. Configure the real `Smtp` settings (host/port/TLS/credentials/from) via secrets/Key Vault.
3. Set real `Cors:AllowedOrigins`, serve over HTTPS, and tune `PasswordHashing:Iterations`.

## Deferred (future phases)

Remaining wizard steps, audit trail, language preference, and the full hybrid existing-member
enrollment verification are intentionally out of this first slice.
