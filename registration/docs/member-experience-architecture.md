# Member Experience & Authorization — Design

> Design doc for the member-facing experience APIs. Separate concern from the registration/identity
> service (which handles Descope sync + JIT migration). Not yet implemented — this captures the agreed
> architecture.

## Context
The mobile app (React Native + Descope) needs member-facing experience APIs: a dashboard, switching the
**view** between self and dependents, switching the active **LOB/plan** being viewed, **role-based**
views, a **CSR** support flow, and a **demo** flow.

Confirmed constraints:
- **Read-only over Facets.** "Change plan" = the user **switches which plan they're viewing**; there are
  **no enrollment writes** to Facets. This layer is query + view-context + authorization only.
- **Descope** owns authentication, roles, sessions, and **CSR impersonation**.
- **LOB is ours** — Descope is LOB-agnostic; we resolve a user's LOB(s) per request.
- **Facets (TriZetto)** is the read-only source of truth for member / subscriber↔dependent / LOB / plan /
  coverage — accessed via **both a Facets service (API) and direct DB reads**.
- Dependents are **mixed**: minors are data-only under the subscriber; adults have their own login.

## Core principle
**Descope owns authentication + coarse, stable roles. We own the domain model + fine-grained,
relationship-based authorization.** Dependent relationships and plan entitlements are dynamic and
relationship-driven, so they must never be baked into IdP claims.

| Concern | Owner |
|---|---|
| AuthN, sessions, MFA/social, roles (Member/CSR/Demo/Admin), CSR impersonation | **Descope** |
| LOB / Plan / Member / Dependent model + coverage | **Facets** (read-only source of truth) |
| LOB resolution, entitlements, view-context, adult-dependent consent, CSR audit, demo isolation | **Us** |

## Request pipeline
Mobile (Descope RN SDK) → session JWT (identity + roles only) → **BFF validates the JWT (Descope .NET
SDK)** → builds an **authorization context**: `actingUser`, `role`, `activeLob`, `subject` (self or a
dependentId), `activePlan`, plus the `actor` (impersonator) claim when a CSR is impersonating → the
**Entitlements service** authorizes → the **Facets adapter** serves/aggregates → response.

The client never asserts access; `subject`/`lob`/`plan` are validated server-side on every request. The
JWT stays lean — entitlements are resolved server-side (and cached), not carried in the token.

## Entitlements / Authorization service (ours)
- `allowedSubjects(user) = {self} ∪ {minor/data-only dependents from Facets} ∪ {adult dependents with an active consent grant}`
- `allowedLobs(user)` and `allowedPlans(user, subject, lob)` resolved from Facets.
Central and reused by every endpoint; caches PHI safely.

## Facets integration (anti-corruption adapter)
`IFacetsMemberProvider` fronts Facets and routes internally: the **Facets service** for authoritative/
business reads (eligibility, coverage), the **DB replica** for high-volume/latency-sensitive lists and
dashboards. Callers don't know which. Centralizes **PHI-safe caching** (short TTL, encrypted, per-user)
and the Facets→domain mapping so Facets' schema never leaks into the BFF.

## Dependents (mixed) + consent (HIPAA-aligned)
- **Minors / data-only:** subscriber/guardian access via the Facets relationship — no separate consent.
- **Adults with their own login:** **default deny.** Subscriber access requires an explicit, revocable
  **consent grant** we store: `{ grantor = adult dependent, grantee = subscriber, scope, status,
  grantedUtc, expiresUtc, revokedUtc }`, audited, minimum-necessary via `scope`.
- The **age threshold** where a dependent flips from data-only to own-identity/consent-required is
  **configurable** (state adolescent-privacy rules vary) — default conservative; confirm with legal.

## CSR flow (Descope impersonation)
CSR authenticates as themselves (role `CSR` + impersonation permission) → **Descope mints a scoped,
time-bound impersonated session** for the member. We (1) read Descope's **actor/impersonator claim** so
the real CSR is always known, (2) keep our **own audit log** (CSR, member, action, time), (3) optionally
**restrict** what an impersonated session may do. The acting party is never lost.

## Demo flow
Descope `Demo` role → **isolated synthetic dataset** (separate demo tenant or a demo-data partition),
**no real PHI**, read-only. Never routes to Facets prod.

## Proposed API surface (read/query BFF)
- `GET /me`, `GET /me/roles`, `GET /me/subjects` (self + accessible dependents), `GET /me/lobs`
- `GET /dashboard?lob=&subject=&plan=`
- `GET /plans?lob=&subject=` (plans available to *view*; "switch plan" = client selects `activePlan`)
- `GET /coverage|benefits|claims?lob=&subject=&plan=`
- Consent (adult dependent manages): `GET/POST /consent/grants`, `DELETE /consent/grants/{id}`
- CSR uses an impersonated session against the same endpoints; every call audited.

Same Clean Architecture, `.NET SDK` JWT validation, ProblemDetails, rate limiting, and feature flags as
the registration service.

## New persistence (our DB, database-first)
- `ConsentGrants` (grantor, grantee, scope, status, granted/expires/revoked, audit fields).
- `CsrAuditEvents` (csrUserId, memberUserId, action, occurredUtc, sourceIp, details).
No Facets data is persisted beyond PHI-safe caching.

## Open items (product / legal / integration)
- Age threshold value(s) and state-specific adolescent-confidentiality rules (legal).
- Consent grant/revoke UX (product).
- Per-read mapping of which Facets reads use the service vs the DB.
- Cache TTLs + invalidation when enrollment changes in Facets.

## SDK split (reference)
- **React Native SDK** (client): interactive auth, session + refresh tokens, secure storage.
- **.NET SDK** (backend): validate the session JWT on every request + Management API.
- Inbound machine-to-machine Descope calls (webhook, JIT verify) use **HMAC signature verification**, not
  JWT validation.
