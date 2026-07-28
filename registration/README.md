# Member Registration API

A **standalone .NET 8** service (Clean Architecture) that backs the portal sign-up flow. It is
intentionally separate from the Auth/login service.

**It is a BFF for a Descope flow, not a public API.** Registration runs as a Descope Flow — the
"passthru" model — and the **Descope engine** calls this service server-to-server through its HTTP
connectors. The mobile app never calls these endpoints; it only renders the flow's screens.

The division of labour:

| | Descope | This service |
| --- | --- | --- |
| Owns | the email address (login ID), the flow and its state, the OTP, the session JWT | the member record, the **password**, eligibility |
| Does | collects input, verifies the OTP, calls us, mints an enriched JWT | stores the record, hashes the password, matches the member in Facets |
| Never sees | the password | — |

```
registration/
  src/
    Domain/          Result/Error primitives, RegistrationErrors, User, PendingRegistration. No dependencies.
    Application/     CQRS use cases (InitiateRegistration, SetPassword, CompleteRegistration), FluentValidation, interfaces, options.
    Infrastructure/  EF Core DbContext (database-first), PBKDF2 hasher, Facets client. 
    Api/             Minimal-API endpoints, connector auth, ProblemDetails, rate limiting, feature flag, Swagger.
  tests/
    Registration.UnitTests/  xUnit tests (validators, handlers, EF repositories, hasher).
```

Dependencies point inward: `Api → Infrastructure → Application → Domain`.

## The flow, and the three calls into this service

Phase numbers match the sequence diagram.

**Phase 1 (Descope only).** The member enters email, name, date of birth and ZIP into the flow's
screens. Descope holds them in flow state and emails an OTP. We are not involved.

**Phase 2 — `POST /api/initiateRegistration`.** Descope validates the OTP, *then* calls us with the
details. We create the member record in **Pending** state and return its `userId`. Descope creates its
own passwordless shadow record (email only) after we succeed, so our record leads and Descope follows.

```jsonc
// request
{ "email": "jane@example.com", "firstName": "Jane", "lastName": "Member",
  "dateOfBirth": "1985-04-23", "zipCode": "12345" }
// 200
{ "userId": "8f3c…", "email": "jane@example.com", "status": "Pending" }
```

A member who abandons the flow and starts again gets the **same** record back rather than a
duplicate-key error; an expired one is replaced. An email that already has a full account is a `409`.

> There is no `emailVerified` flag anywhere in this service. Descope only calls us after verifying the
> OTP, so a record existing here already means the address was verified — a guarantee that rests
> entirely on the connector credential below.

**Phase 3 — `POST /api/registration/password`.** The password, hashed with PBKDF2 and stored against
the pending record. Descope never receives it, which is why sign-in has to be validated against this
database.

```jsonc
{ "userId": "8f3c…", "password": "…", "confirmPassword": "…" }   // -> 200
```

The diagram shows this hitting `/api/initiateRegistration` again with a different body; that reads as a
copy-paste slip, so it has its own route. If it really must share the path, say so and it can be
folded back.

**Phase 4 — `POST /api/completeRegistration`.** The SSN (and optional member ID) are matched against
Facets across all tenants. On a match the pending record is promoted to a real `User` — carrying the
**same id** — and the subscriber and plan come back for Descope to map into the session JWT's custom
claims.

```jsonc
// request
{ "email": "jane@example.com", "ssn": "123-45-6789", "memberId": null }
// 200
{ "complete": true, "userId": "8f3c…",
  "memberInfo": { "subscriberId": "SUB1234", "memberId": "MBR1234", … },
  "planInfo":   { "planId": "PLN1234", "planName": "…", … } }
```

`memberInfo.subscriberId` and `planInfo.planId` are part of the contract with the flow — the connector
maps those paths into claims, so renaming them means reconfiguring Descope.

## Authentication: the connector key

These endpoints are machine-to-machine. There is no member session token to validate — the caller is
the Descope engine — so a shared secret in a header is what separates a real connector call from
anyone who found the URL.

```jsonc
"ConnectorAuth": {
  "HeaderName": "X-Connector-Key",
  "Keys": [ "…" ],          // supply via secrets/Key Vault; two at once allows zero-downtime rotation
  "AllowAnonymous": false   // development only
}
```

Startup fails if neither a key nor `AllowAnonymous` is configured. Comparison is constant-time and does
not short-circuit on the first match.

This key is load-bearing in a way that is easy to miss: it is the *only* evidence this service has that
an email address was verified. Treat it like a signing key.

## Eligibility (Facets)

`IFacetsClient` has two implementations, chosen by `Facets:Provider`:

- **`Http`** — the real lookup. ⚠️ Its request/response DTOs were written from the sequence diagram,
  **not** from a published Facets contract, and are marked provisional in the source. Align them before
  trusting them; the surrounding behaviour (auth header, timeout, status mapping) holds regardless.
- **`Stub`** — matches everyone, for walking the flow before that integration exists. Startup **throws**
  if it is selected outside Development. An SSN ending `0000` returns "not found" so the unhappy path
  is reachable.

Two failure modes are kept distinct all the way to the caller, because they mean different things to a
member: *no such member* (their details are wrong) versus *lookup failed* (we're broken, try later).

A Facets match on SSN alone is **not** enough to hand over an account — surname and date of birth must
also agree with what was registered. A mismatch returns exactly the same error as "not found", so the
response can't be used to probe whose SSN it is; the real reason is logged.

## Handling of SSN

The full SSN is used for the Facets match and then **discarded**. Only `SsnLast4` is persisted. It is
never logged, and error responses from the Facets client deliberately omit the response body in case it
echoes the number back.

## Running it

```bash
cd registration
dotnet run --project src/Api            # Development: InMemory DB + Facets stub + dev connector key
```

`requests.http` walks all three phases in order, including the 401 and not-eligible paths.

For a real database, run `scripts/create-registration-user-table.sql` once, then set
`Database:Provider = SqlServer` and `ConnectionStrings:RegistrationDb`.

Abandoned pending records need purging on a schedule — the DDL script has the statement. That matters
more than it looks: `PendingRegistrations.Email` is unique, so an abandoned row holds that address
until it is removed (the initiate handler clears an expired one it finds, but only when that member
comes back).

## What is deliberately NOT here

- **OTP and email delivery.** Descope owns both. The previous version of this service generated and
  mailed its own codes; that is gone.
- **Sign-in.** Still the Auth API's job. Note that it will not work for members registered this way
  until password validation is proxied to this database — the password lives here now.
