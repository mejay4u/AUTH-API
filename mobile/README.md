# Member Portal — Mobile App

A React Native (Expo, TypeScript) app for the member portal auth API. A member signs in with
**User ID + password**, lands on a home screen showing the **single sign-on portals** (e.g.
**HRA**), and taps one to be **logged straight into that portal** — no second password.

## The flow

The app drives the real three-call flow (the API served with Scalar docs at `localhost:38340`):

1. **Initiate login** → `POST /api/v1/auth/login`

   ```jsonc
   { "TransId": "", "UserId": "<user id / email>", "Password": "<password>",
     "AppId": "LAEX", "PlanId": "LAEX", "Entity": 2, "Lang": "en", "Version": "2",
     "SessionKey": "" }
   ```

   Validates the credentials and returns the **member envelope** (TransId, MemberId, name,
   DOB, email, phones, …).

2. **Complete login** → `POST /api/v1/auth/completelogin`

   The app posts the envelope from step 1 straight back. The response carries the JWT
   **`securityToken`** plus display fields (`memberRole`, `gender`, `emailId`, …). The token is
   stored in the device keychain (`expo-secure-store`).

3. **SSO launch** → `GET /api/v1/sso?lob=LAEX&ssoName=HRA&planCode=LAEX`
   with `Authorization: Bearer <securityToken>`

   Returns the complete PingFederate sign-on URL in **`ssoUrl`**
   (`.../idp/startSSO.ping?...&opentoken=...`). The app opens it in an in-app WebView and the
   portal (HRA, Softheon, …) logs the member straight in.

Steps 1→2 run back-to-back on the login button; step 3 runs when a portal card is tapped.

## Run it

```bash
cd mobile
npm install
npm start          # then press i (iOS), a (Android), or scan the QR in Expo Go
```

Point the app at your API in **`src/config/index.ts`** (`defaultApiBaseUrl`, default
`http://localhost:38340`) or in `app.json > expo.extra.apiBaseUrl`. You can also change it at
runtime under **Advanced · server settings** on the login screen.

> On a **physical device**, `localhost` means the phone. Use your computer's LAN IP, e.g.
> `http://192.168.1.20:38340`, and make sure the API is reachable there.

Example credentials from the shared Postman calls: User ID
`sshahi+6702091900@amerihealthcaritas.com`.

## Where to adjust the contract

Everything API-shaped lives in a few files so it's easy to tweak against your environment:

| Thing | File | Notes |
|-------|------|-------|
| Base URL, endpoint paths | `src/config/index.ts` | `defaultApiBaseUrl`, `endpoints` |
| Tenant constants | `src/config/index.ts > auth` | `AppId`/`PlanId`/`Entity`/`Lang`/`Version` sent in the login envelope (default `LAEX` / `2` / `en` / `2`) |
| Login request fields | `src/api/auth.ts` | `initiateLogin()` builds the `POST /auth/login` body |
| Envelope forwarded to completelogin | `src/api/auth.ts` | `completeLogin()` forwards the login response as-is |
| SSO portal catalog | `src/config/index.ts > ssoPortals` | Each entry's `ssoName` + `lob` (+ `planCode`) are sent to `GET /api/v1/sso` |

The API has **no "list SSOs" endpoint**, so the portals shown after login come from
`ssoPortals` (HRA and SoftheonSSO by default). `ssoUrl` can be **null** (skipped LOB / no URL
provider) — the app shows a friendly "not available" message; a **404** means that
`lob` + `ssoName` pairing isn't configured. To auto-close the WebView when a portal finishes
login, add its post-login landing URL prefix to `config.ssoSuccessUrlPrefixes`; otherwise the
member taps **Done**.

## Project layout

```
mobile/
  App.tsx                     Providers + navigation container
  src/
    config/index.ts           Base URL, endpoints, tenant constants, portal catalog  ← edit here
    api/                       http wrapper, auth (login + completelogin), sso, types
    auth/                      AuthContext (session) + secure storage
    navigation/                stack navigator (Login | Home + SSO modal)
    screens/                   LoginScreen, HomeScreen, SsoWebViewScreen
    components/                Button, Field, PortalCard
    theme.ts                   colors / spacing
```

## Notes

- `npm run typecheck` runs `tsc --noEmit`.
- The `securityToken` lives only in the OS secure store (localStorage on web). There is no
  refresh endpoint wired, so an expired/invalid token prompts a fresh sign-in.
- Field casing: the login request mirrors the working Postman body (PascalCase); ASP.NET
  binds case-insensitively, so the camelCase completelogin/SSO responses map cleanly.
- Built against Expo SDK 51 / React Native 0.74.
