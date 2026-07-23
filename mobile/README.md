# Member Portal — Mobile App

A React Native (Expo, TypeScript) app for the Member Auth API. A member signs in with
**username + password**, lands on a home screen showing the **single sign-on portals**
available to them (e.g. **HRA**), and taps one to be **logged straight into that portal**
— no second password.

## What it does

1. **Login** → `POST /api/v1/auth/login` with `{ username, password, lob }`. Stores the
   access + refresh tokens in the device keychain (`expo-secure-store`).
2. **Home** → loads the member (`GET /api/v1/members/me`) and shows the configured SSO
   portals (e.g. **HRA**) as tappable cards.
3. **SSO launch** → tapping a portal calls `GET /api/v1/sso?lob=&ssoName=`, which returns
   the **complete PingFederate sign-on URL** (OpenToken already appended). The app opens
   that URL in an in-app WebView and the portal (e.g. HRA) logs the member straight in.
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

## SSO contract this app targets

The app matches the federated-SSO endpoint from the Auth API's SSO work (the
`claude/pingfed-url-completion-*` branch — a clean-architecture rewrite of the legacy
`MemberController.GetSSO`). It's a **single GET**, not an initiate/complete pair:

```
GET /api/v1/sso?lob={lob}&ssoName={ssoName}&planCode={optional}&dateOfBirth={optional}
Authorization: Bearer <access token>
```

Member identity (id, email, name, dependents, birthdate) is read by the API from the
**validated JWT**, so the client never sends who is signing on. The response:

```jsonc
{
  "memberId": "...",
  "role": "Member",
  "lob": "2100",
  "ssoName": "HRA",
  "assessmentName": "AdultAssessment",
  "ssoUrl": "https://pingfed.example.com/sso/...&opentoken=...",  // open this in the WebView
  "data": [ { "ssoName": "HRA", "pingFedUrl": "...", "pingFedReturnUrl": "...", "...": "..." } ]
}
```

The app opens `ssoUrl` in a WebView. `ssoUrl` can be **null** (a skipped LOB, or an SSO name
with no URL provider) — the app then shows a friendly "not available" message. A **404** means
that `lob` + `ssoName` pairing isn't configured server-side.

Because the API has **no "list SSOs" endpoint**, the portals shown after login come from the
catalog in **`src/config/index.ts > ssoPortals`**. Each entry declares the `ssoName`, the
numeric `lob`, an optional `planCode`, and display text. Valid SSO names: `HRA`, `CHATSSO`,
`CERTIFISSO`, `ABARCASSO`, `SOFTHEONSSO`, `PLANOFCARESSO`, `SDS`. Edit this list to match the
portals configured for your members. To auto-close the WebView when a portal finishes login,
add its post-login landing URL prefix to `config.ssoSuccessUrlPrefixes`; otherwise the member
taps **Done**.

> The SSO backend lives on the `claude/pingfed-url-completion-*` branch and requires
> `ConnectionStrings:MemberDb` plus the PingFederate agent files to generate real URLs. Run the
> API from that branch for end-to-end SSO; login/refresh/me work against `main` too.

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
