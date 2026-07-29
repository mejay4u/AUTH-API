# Member Registration API

A **standalone .NET 8** service (Clean Architecture) that backs the portal sign-up wizard. It is
intentionally separate from the Auth/login service.

**Descope verifies the email; this service owns everything else.** The mobile app runs the wizard,
uses the Descope SDK for the email OTP, then calls these two endpoints with the session token Descope
issued. The member record and the **password** live here, not in Descope.

| | Descope | This service |
| --- | --- | --- |
| Owns | the email address (login ID), the OTP, the session token | the member record and the **password** |
| Never sees | the password, or any profile field | — |

```
registration/
  src/
    Domain/          Result/Error primitives, RegistrationErrors, User, PendingRegistration. No dependencies.
    Application/     CQRS use cases (InitiateRegistration, CreateAccount), FluentValidation, interfaces, options.
    Infrastructure/  EF Core DbContext (database-first), PBKDF2 hasher.
    Api/             Minimal-API endpoints, Descope token validation, ProblemDetails, rate limiting, feature flag, Swagger.
  tests/
    Registration.UnitTests/  xUnit tests (validators, handlers, EF repositories, hasher).
```

Dependencies point inward: `Api → Infrastructure → Application → Domain`.

## The wizard, and the two calls into this service

The app's six-step wizard: **Personal Information → Verify Email → Review → Create Account → (5,
membership check — not built yet) → All set.**

Steps 1–2 are Descope only: the app collects the details and calls `otp.signUp.email` /
`otp.verify.email`. Nothing reaches this service until the email is verified.

**Step 3 — `POST /api/initiateRegistration`.** The member has confirmed their details on the review
screen. Store them and return the pending record's id.

```jsonc
// request
{ "email": "jane@example.com", "firstName": "Jane", "lastName": "Member",
  "dateOfBirth": "1985-04-23", "zipCode": "12345", "contactNumber": "123-456-7890" }
// 200
{ "userId": "8f3c…", "email": "jane@example.com", "status": "Pending" }
```

A member who abandons the wizard and starts again gets the **same** record back rather than a
duplicate-key error; an expired one is replaced. An email that already has an account is a `409`.

**Step 4 — `POST /api/registration/password`.** The Create Account button. Hash the password, promote
the pending record to a real `User` under the **same id**, and delete the pending row.

```jsonc
// request
{ "userId": "8f3c…", "password": "…", "confirmPassword": "…" }
// 201
{ "userId": "8f3c…", "email": "jane@example.com", "username": "jane@example.com" }
```

The password never goes to Descope, which is why sign-in has to be validated against this database.

> There is no `emailVerified` flag anywhere in this service. Descope verifies the address before the
> app is given the token that authorises these calls, so a pending record existing at all means the
> address was verified — provided the token is validated, which is the next section.

## Authentication

Both endpoints require the **Descope session JWT** the app received from `otp.verify.email`, sent as
`Authorization: Bearer …`. It is validated properly — signature against Descope's JWKS for the
project, issuer, and lifetime — not merely decoded.

```jsonc
"Descope": {
  "ProjectId": "P2xxxxxxxx",          // also the token issuer
  "BaseUrl": "https://api.descope.com",
  "AllowAnonymous": false             // development only
}
```

Startup fails if no project ID is configured and `AllowAnonymous` is off.

`initiateRegistration` additionally checks that the email in the body matches the one in the token, so
a valid token for one address can't register another. That check is skipped when the token carries no
email claim — Descope projects vary on this, so if yours doesn't include one, either add it as a
custom claim or resolve it with the Management SDK before relying on the check.

## Password policy

Defaults match the Create Account screen's checklist: **14–56 characters**, with uppercase, lowercase,
digit and special all required. Configurable under `PasswordPolicy` — change it here and in the app's
`PASSWORD_POLICY` together, or members will be told one thing and refused for another.

Hashing is PBKDF2 (HMAC-SHA256, 210k iterations by default) behind `IPasswordHasher`, so the scheme
can be swapped without touching a use case.

## Running it

```bash
cd registration
dotnet run --project src/Api            # Development: InMemory DB, token validation off
```

`requests.http` walks both steps in order, including the resume path and a rejected password.

For a real database, run `scripts/create-registration-user-table.sql` once, then set
`Database:Provider = SqlServer` and `ConnectionStrings:RegistrationDb`.

Abandoned pending records need purging on a schedule — the DDL script has the statement. That matters
more than it looks: `PendingRegistrations.Email` is unique, so an abandoned row holds that address
until it is removed (the initiate handler clears an expired one it finds, but only when that member
comes back).

## What is deliberately NOT here

- **OTP and email delivery.** Descope owns both.
- **The membership/eligibility check** (SSN, Facets lookup, subscriber and plan IDs). That's step 5 of
  the design and isn't built — the wizard currently goes straight from Create Account to the success
  screen. An earlier draft of it is on the `claude/registration-descope-passthru` branch.
- **Sign-in.** Still the Auth API's job, and note it will not work for members registered this way
  until password validation is pointed at this database — the password lives here now.
