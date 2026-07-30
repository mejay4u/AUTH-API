# Member Registration API

A **standalone .NET 8** service (Clean Architecture) that backs the portal sign-up flow. It is
intentionally separate from the Auth/login service.

**Descope verifies the email; this service owns everything else.** Registration runs as a **Descope
Flow** — the flow's screens collect the details and verify the email, and the **Descope engine** calls
these two endpoints through HTTP connectors. The mobile app only hosts the flow; it never calls this
service. The member record and the **password** live here, not in Descope.

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
    Api/             Minimal-API endpoints, connector-key auth, ProblemDetails, rate limiting, feature flag, Swagger.
  tests/
    Registration.UnitTests/  xUnit tests (validators, handlers, EF repositories, hasher).
```

Dependencies point inward: `Api → Infrastructure → Application → Domain`.

## The flow, and the two calls into this service

The Descope flow's phases: **collect details → send OTP → verify OTP → `initiateRegistration` →
create the shadow user → password screen → `registration/password` → issue session.** The
membership/eligibility phase is deferred.

Nothing reaches this service until the flow has verified the email.

**Call 1 — `POST /api/initiateRegistration`.** Fired by the flow's connector immediately after the OTP
is verified. Store the details held in flow state and return the pending record's id.

```jsonc
// request
{ "email": "jane@example.com", "firstName": "Jane", "lastName": "Member",
  "dateOfBirth": "1985-04-23", "zipCode": "12345", "contactNumber": "123-456-7890" }
// 200
{ "userId": "8f3c…", "email": "jane@example.com", "status": "Pending" }
```

A member who abandons the flow and starts again gets the **same** record back rather than a
duplicate-key error; an expired one is replaced. An email that already has an account is a `409`.

**Call 2 — `POST /api/registration/password`.** Fired after the flow's password screen. Hash the
password, promote the pending record to a real `User` under the **same id**, and delete the pending
row. The flow then issues the session.

```jsonc
// request
{ "userId": "8f3c…", "password": "…", "confirmPassword": "…" }
// 201
{ "userId": "8f3c…", "email": "jane@example.com", "username": "jane@example.com" }
```

The password never goes to Descope, which is why sign-in has to be validated against this database.

> There is no `emailVerified` flag anywhere in this service. The flow verifies the address before its
> connector makes call 1, so a pending record existing at all means the address was verified — provided
> the connector key is validated, which is the next section.

## Authentication

Both endpoints are called **machine to machine by Descope's flow connectors**, not by the app and not
by a member. There is no session token to validate — a shared secret in a header is what separates a
real connector call from anyone who found the URL.

```jsonc
"ConnectorAuth": {
  "HeaderName": "X-Connector-Key",
  "Keys": [ "…" ],          // supply via secrets/Key Vault; two at once allows zero-downtime rotation
  "AllowAnonymous": false   // development only
}
```

Startup fails if neither a key nor `AllowAnonymous` is configured. Comparison is constant-time and
does not short-circuit on the first match.

This key is load-bearing in a way that's easy to miss: because the flow only calls these endpoints
*after* verifying the OTP, the key is the **only** evidence this service has that an email address was
verified. Treat it like a signing key.

## Password policy

Defaults match the Create Account screen's checklist: **14–56 characters**, with uppercase, lowercase,
digit and special all required. Configurable under `PasswordPolicy` — change it here and in the app's
`PASSWORD_POLICY` together, or members will be told one thing and refused for another.

Hashing is PBKDF2 (HMAC-SHA256, 210k iterations by default) behind `IPasswordHasher`, so the scheme
can be swapped without touching a use case.

## Running it

```bash
cd registration
dotnet run --project src/Api            # Development: InMemory DB + a fixed dev connector key
```

`requests.http` walks both calls in order, including the resume path, a rejected password and the 401
path with no connector key.

For a real database, run `scripts/create-registration-user-table.sql` once, then set
`Database:Provider = SqlServer` and `ConnectionStrings:RegistrationDb`.

Abandoned pending records need purging on a schedule — the DDL script has the statement. That matters
more than it looks: `PendingRegistrations.Email` is unique, so an abandoned row holds that address
until it is removed (the initiate handler clears an expired one it finds, but only when that member
comes back).

## What is deliberately NOT here

- **OTP and email delivery.** Descope owns both.
- **The membership/eligibility check** (SSN, Facets lookup, subscriber and plan IDs). Phase 4 of the
  diagram; not built. The flow currently ends after the password call. An earlier draft is in this
  branch's history.
- **Sign-in.** Still the Auth API's job, and note it will not work for members registered this way
  until password validation is pointed at this database — the password lives here now.
