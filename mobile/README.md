# Member Portal — Mobile App

A React Native (Expo, TypeScript) app for the Member Auth API. A member signs in with
**username + password**, lands on a home screen showing the **single sign-on portals**
available to them (e.g. **HRA**), and taps one to be **logged straight into that portal**
— no second password.

## What it does

1. **Login** → `POST /api/v1/auth/login` with `{ username, password, lob }`. Stores the
   access + refresh tokens in the device keychain (`expo-secure-store`).
2. **Home** → loads the member (`GET /api/v1/members/me`) and the SSO portal list, then
   shows each portal as a tappable card.
3. **SSO launch** → tapping a portal calls the **initiate login** endpoint, then opens the
   returned URL in an in-app WebView to **complete the logon** at the portal (e.g. HRA).
4. **Sessions** are restored on relaunch and the access token is **auto-refreshed** via the
   rotating refresh token when it expires.

## Run it

```bash
cd mobile
npm install
npm start          # then press i (iOS), a (Android), or scan the QR in Expo Go
```

Point the app at your API in **`src/config/index.ts`** (`defaultApiBaseUrl`) or in
`app.json > expo.extra.apiBaseUrl`. You can also change it at runtime under
**Advanced · server settings** on the login screen.

> On a **physical device**, `localhost` means the phone. Use your computer's LAN IP, e.g.
> `http://192.168.1.20:5155`, and make sure the API is reachable on that address.

Demo members seeded by the API in Development: `jdoe` / `P@ssw0rd!` (DENTAL, VISION),
`asmith` / `Secret123!` (MEDICAL).

## SSO contract this app expects

The login/refresh/me endpoints already exist in the .NET API. The SSO endpoints are wired
to a small, configurable contract (edit routes in `src/config/index.ts`):

| Call            | Route (default)                        | Request                 | Response |
|-----------------|----------------------------------------|-------------------------|----------|
| Initiate login  | `POST /api/v1/sso/{portal}/initiate`   | Bearer + `{ portal }`   | `SsoLaunch` |
| Complete logon  | `POST /api/v1/sso/{portal}/complete-logon` | Bearer + `{ portal }` | *(optional; 200)* |
| Portal list     | `GET /api/v1/sso/portals`              | Bearer                  | `Portal[]` *(optional)* |

`SsoLaunch` supports both common SSO binding styles:

```jsonc
// 1) Redirect / GET binding — the app just opens `url`
{ "portal": "HRA", "method": "GET", "url": "https://hra.example.com/sso/start?token=..." }

// 2) SAML / form POST binding — the app auto-submits a hidden form to `url`
{
  "portal": "HRA",
  "method": "POST",
  "url": "https://hra.example.com/saml/acs",
  "formFields": { "SAMLResponse": "base64...", "RelayState": "..." }
}
```

If `GET /api/v1/sso/portals` isn't implemented (404), the app falls back to the catalog in
`config.fallbackPortals` — which includes **HRA** — so the links still appear. To auto-close
the WebView when a portal finishes login, add the portal's post-login landing URL prefix to
`config.ssoSuccessUrlPrefixes`; otherwise the member taps **Done**.

## Project layout

```
mobile/
  App.tsx                     Providers + navigation container
  src/
    config/index.ts           API base URL, endpoint routes, portal catalog  ← edit here
    api/                       http wrapper, auth, sso, shared types
    auth/                      AuthContext (session + token refresh) + secure storage
    navigation/                stack navigator (Login | Home + SSO modal)
    screens/                   LoginScreen, HomeScreen, SsoWebViewScreen
    components/                Button, Field, PortalCard
    theme.ts                   colors / spacing
```

## Notes

- `npm run typecheck` runs `tsc --noEmit`.
- No secrets are bundled; tokens live only in the OS secure store (localStorage on web).
- Built against Expo SDK 51 / React Native 0.74.
