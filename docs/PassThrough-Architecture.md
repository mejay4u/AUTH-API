# BFA pass-through architecture

## Why this exists

The Auth API was built to authenticate on its own: read the member record out of the LOB database,
verify the password, mint an RS256 token. That design assumes the process can reach the on-prem
member databases.

It cannot. The API runs in ARO, and ARO has no route to the on-prem DB.

Rather than build that route, we reuse the .NET **ARTS** auth service that already runs on-prem and
already has DB access. The service in ARO stops being an authenticator and becomes a **BFA** — a
pass-through that carries the call to ARTS and carries the answer back.

```
Member Portal
     │  POST /api/v1/auth/login
     ▼
Apigee External  ── edge: TLS, WAF, client credentials, quota
     │
     ▼
   BFA (ARO)     ── this repository, Auth:Mode = PassThrough
     │              validates shape, rate-limits, correlates, relays
     ▼
Apigee Internal  ── internal: mTLS / API key, on-prem routing
     │
     ▼
ARTS (.NET, on-prem)  ── authenticates, issues tokens
     │
     ▼
  On-prem member DB
```

## The one decision that shapes everything else

**Where does the auth contract live?**

Two options were on the table:

| | Typed relay | Opaque relay *(chosen)* |
|---|---|---|
| Body handling | Deserialize to a BFA DTO, re-serialize upstream | Forward the client's bytes verbatim |
| Response | Map ARTS's payload onto a BFA response type | Replay ARTS's status, body and headers |
| Adding a login field | Needs a BFA release | Needs no BFA change |
| Field ARTS knows and BFA doesn't | **Silently dropped** | Survives |
| BFA can reshape/enrich the payload | Yes | No |

The opaque relay wins because the alternative's failure mode is silent. A typed relay that
deserializes into `LoginRequest(Username, Password, Lob)` and re-serializes will quietly discard a
`deviceFingerprint` the portal and ARTS agreed on, and nothing in the system reports it — the login
just behaves differently than either end expects.

So the BFA forwards bytes. It still parses the body *for validation*, but it never rebuilds it.

The trade-off is real and worth stating: the BFA cannot enrich or reshape the payload. If a future
requirement needs that (merging in profile data, say), that endpoint stops being a pass-through and
gets a typed slice of its own. The seam is the same either way, which is the point.

## How it maps onto the layers

The existing clean-architecture layering is unchanged. Pass-through is a second implementation of
the same use case, not a second architecture.

```
Api            AuthEndpoints (mode branch) ─ PassThroughRequestReader ─ UpstreamRelayResult
                    │  reads HttpContext, builds a transport-neutral request
                    ▼
Application    ForwardLoginCommand ─► ForwardLoginCommandHandler ─► IUpstreamAuthGateway   ◄── the port
                    │  validation, logging; knows nothing about HTTP
                    ▼
Infrastructure ApigeeInternalAuthGateway (typed HttpClient)                                ◄── the adapter
                    CorrelationIdHandler · TransientFaultHandler · mTLS · API key
```

The dependency rule holds: `IUpstreamAuthGateway` is declared in Application, implemented in
Infrastructure. The Application layer never learns that the upstream is HTTP, or that Apigee is in
the path at all.

### Where each concern lives

| Concern | Home | Why there |
|---|---|---|
| Reading the raw body, headers, client IP | `Api/PassThrough/PassThroughRequestReader` | The only place allowed to touch `HttpContext` |
| Shape validation | `ForwardLoginCommandValidator` | Same pipeline as every other use case |
| Deciding *what* to relay | `ForwardLoginCommandHandler` | The use case |
| HTTP, retries, mTLS, API key | `Infrastructure/Gateways/Apigee` | Transport detail |
| Replaying upstream's response | `Api/PassThrough/UpstreamRelayResult` | Framework detail |

## Result semantics at the gateway boundary

This is the subtle part, and getting it backwards produces a BFA that turns every rejected login
into a 500.

```
Result.IsSuccess  →  ARTS answered. Any status: 200, 401, 403, 423.
Result.IsFailure  →  We never got an answer. Only two shapes: Unavailable, Timeout.
```

A 401 from ARTS is a **successful relay**. Relaying it is the job. The only failures the BFA
authors itself are `503` (couldn't reach Apigee Internal) and `504` (no answer in time) — plus a
`400` for a body that isn't well-formed JSON.

## What pass-through mode deliberately does *not* register

In `Auth:Mode = PassThrough`, the composition root registers the Apigee gateway and nothing else —
no `DbContext`, no `IAccountRepository`, no `IPasswordHasher`, no `RsaSigningKeyProvider`, no JWT
bearer validation, no JWKS endpoint.

That absence is load-bearing:

- **It fails at startup, not at first login.** A pod configured for a database it cannot reach never
  passes its readiness probe.
- **The BFA holds no signing key and no DB credential.** A compromised BFA pod can replay traffic;
  it cannot mint a token or read a member record. The secrets stay on-prem with ARTS.
- **No JWKS.** ARTS is the issuer, so publishing a key set here would advertise a key that signs
  nothing. Downstream services must point their `Authority` at ARTS.

## Retry policy — why it is so conservative

Login and refresh are **not idempotent**. A retried login burns a lockout attempt; a retried refresh
can consume a rotating token whose replacement the client never received.

So `TransientFaultHandler` retries only calls it can prove never reached ARTS:

| Condition | Retry? | Reasoning |
|---|---|---|
| Connection failure | Yes | Never made it onto the wire |
| 502 / 503 from Apigee | Yes | The proxy is saying it didn't reach ARTS |
| **Timeout** | **No** | We stopped waiting; ARTS may well have succeeded |
| **504** | **No** | The gateway timed out on a request it *did* forward |
| 4xx / 5xx from ARTS | No | That's an answer — relay it |

Backoff is exponential with **full jitter**, so an Apigee blip doesn't turn a burst of logins into a
synchronised retry storm against a single on-prem service.

## Known gap: the rate limiter partitions on the wrong IP

`RateLimiterPolicies.Auth` partitions on `HttpContext.Connection.RemoteIpAddress`. Behind Apigee
External that is **Apigee's** address, not the caller's — so every request collapses into a single
partition and the limit behaves as a crude global cap rather than the per-client throttle it reads as.

This predates the pass-through, but it matters more here, because the BFA's rate limiter is the last
thing standing between an internet-facing edge and a single on-prem service.

The fix is `UseForwardedHeaders` with `KnownProxies`/`KnownNetworks` set to the Apigee egress ranges,
so `RemoteIpAddress` reflects `X-Forwarded-For`. It is deliberately **not** done here: enabling it
without a pinned list of trusted proxies lets any caller spoof its own partition key, which is worse
than the current behaviour. It needs the actual egress ranges from the Apigee team.

Until then, treat the BFA's limiter as a blunt total-throughput cap and rely on Apigee External's
quota policy for per-client limits.

## Headers

A header must clear **two** gates to reach ARTS: the API layer's deny-list
(`PassThroughRequestReader`) and the gateway's allow-list (`ApigeeInternal:ForwardRequestHeaders`).

Never forwarded upstream: `Authorization`, `Cookie`, `x-api-key`, `Host`, and all framing headers.
Edge credentials are scoped to the edge — Apigee External's client credential must not be replayed
to Apigee Internal, which has its own.

Two things are added on the way out:

- **`X-Forwarded-For`**, appended with the caller's IP, so ARTS's per-origin lockout counts the real
  client rather than the BFA pod.
- **`X-Correlation-Id`**, honoured from the caller if present (sanitised and length-capped, since it
  reaches log lines and an outbound header) or minted. One id spans Portal → BFA → Apigee → ARTS.

Coming back down, only `ApigeeInternal:ForwardResponseHeaders` are relayed, minus framing headers,
which Kestrel owns for the downstream connection.

## Configuration

```jsonc
{
  "Auth": {
    "Mode": "PassThrough",          // Local | PassThrough
    "MaxRequestBodyBytes": 32768
  },
  "ApigeeInternal": {
    "BaseAddress": "https://internal-apigee.corp/arts/v1/",
    "LoginPath": "auth/login",
    "RefreshPath": "auth/refresh",
    "TimeoutSeconds": 15,
    "MaxRetryAttempts": 2
  }
}
```

Secrets — `ApigeeInternal:ApiKey`, `ApigeeInternal:ClientCertificatePassword` — come from
OpenShift secrets or Key Vault, never from `appsettings.json`. In OpenShift, as environment
variables:

```
Auth__Mode=PassThrough
ApigeeInternal__BaseAddress=https://internal-apigee.corp/arts/v1/
ApigeeInternal__ApiKey=<from secret>
```

Keep `ApigeeInternal:TimeoutSeconds` comfortably below the timeout Apigee External applies to the
BFA, so the client receives the BFA's `504` (with a correlation id) rather than the edge's generic one.

## Cut-over

Both modes ship in the same image; `Auth:Mode` selects between them. That makes the cut-over a
config change and the rollback a config change — no rebuild, no separate artifact.

1. Deploy with `Auth:Mode=Local` (current behaviour, unchanged).
2. Point a non-production instance at Apigee Internal with `Auth:Mode=PassThrough`.
3. Flip production.
4. Roll back by flipping it back.

Once ARTS is the permanent issuer, the local slice (`LoginCommandHandler`, `RsaSigningKeyProvider`,
the LOB repositories) can be deleted and `AuthMode` collapsed away. Until then it stays as the
rollback path.

## Open items for the ARTS/Apigee owners

These need answers from the teams that own the upstream, and each one is a small, contained change:

- **Exact ARTS paths and payload shape.** `LoginPath`/`RefreshPath` default to `auth/login` and
  `auth/refresh`; the validator assumes `username` / `password` / `lob` and `refreshToken`. If ARTS
  differs, only `ApigeeInternalOptions` and the two validators change.
- **Apigee Internal credential.** API key header vs. mTLS vs. OAuth client credentials. The first two
  are wired; a client-credentials flow would be one more `DelegatingHandler` in the same pipeline.
- **Does ARTS return tokens this JWT contract can validate?** If downstream services currently trust
  this API's JWKS, they need repointing at ARTS's issuer.
- **Whether refresh stays in ARTS.** Assumed yes — rotation and revocation need the DB.
- **Apigee External's egress IP ranges**, so the rate limiter can partition on the real client — see
  the known gap above.
