import Constants from 'expo-constants';

import type { SsoPortal } from '../api/types';

/**
 * Central configuration for the Member Portal mobile app.
 *
 * The app talks to the .NET Auth API (`src/Api`). Point `apiBaseUrl` at wherever that
 * API is running. On a physical device you MUST use the machine's LAN IP (e.g.
 * `http://192.168.1.20:5155`), not `localhost` — `localhost` on a phone means the phone.
 *
 * The default is read from `app.json > expo.extra.apiBaseUrl` so it can be changed
 * without touching code, and can still be overridden at runtime on the login screen.
 */
const extra = (Constants.expoConfig?.extra ?? {}) as { apiBaseUrl?: string };

export const config = {
  /** Base URL of the Auth API. Overridable at runtime from the login screen. */
  defaultApiBaseUrl: extra.apiBaseUrl ?? 'http://localhost:5155',

  /** Auth API routes. */
  endpoints: {
    login: '/api/v1/auth/login',
    refresh: '/api/v1/auth/refresh',
    me: '/api/v1/members/me',

    /**
     * Federated SSO. `GET /api/v1/sso?lob=&ssoName=&planCode=` (Bearer) returns an
     * `SsoResponse` whose `ssoUrl` is the complete PingFederate sign-on URL to open.
     */
    sso: '/api/v1/sso',
  },

  /**
   * Lines of business the login screen lets the member pick from. `login` requires a
   * `lob` — it selects which line-of-business database to authenticate against. (This is
   * separate from the numeric SSO `lob` codes used by the portal catalog below.)
   */
  lobs: ['DENTAL', 'VISION', 'MEDICAL', 'RX'] as const,

  /**
   * SSO portal catalog shown after login. The Auth API has no "list SSOs" endpoint, so the
   * portals a member can launch are declared here. Each entry's `ssoName` + `lob` (+ optional
   * `planCode`) are sent to `GET /api/v1/sso`; a 404 from the API means that pairing isn't
   * configured server-side and the app surfaces a friendly message.
   *
   * `ssoName` must be one the API knows: HRA, CHATSSO, CERTIFISSO, ABARCASSO, SOFTHEONSSO,
   * PLANOFCARESSO, SDS. `lob` is the numeric LOB code the SSO config is keyed by (e.g. 2100).
   * HRA is the worked example from the requirements.
   */
  ssoPortals: [
    {
      ssoName: 'HRA',
      lob: '2100',
      name: 'HRA Portal',
      description: 'Health Reimbursement Account — balances, claims & reimbursements.',
      accent: '#34D399',
    },
    {
      ssoName: 'CHATSSO',
      lob: '2100',
      name: 'Member Chat',
      description: 'Chat with a benefits specialist, signed in automatically.',
      accent: '#60A5FA',
    },
    {
      ssoName: 'CERTIFISSO',
      lob: '2100',
      name: 'Payments (Certifi)',
      description: 'Pay premiums and manage billing.',
      accent: '#A78BFA',
    },
  ] as SsoPortal[],

  /**
   * When an SSO WebView navigates to a URL that starts with any of these, the app treats
   * the portal login as complete and closes the WebView. Tune to your portals' post-login
   * landing pages. Empty array = never auto-close (user taps Done).
   */
  ssoSuccessUrlPrefixes: [] as string[],
};

export type LobCode = (typeof config.lobs)[number];
