import Constants from 'expo-constants';

import type { SsoPortal } from '../api/types';

/**
 * Central configuration for the Member Portal mobile app.
 *
 * The app drives the member portal auth API (the one served with Scalar docs at
 * `localhost:38340`): a two-step login (`/auth/login` then `/auth/completelogin`) that
 * issues a JWT `securityToken`, followed by `GET /api/v1/sso` for federated sign-on.
 *
 * Point `apiBaseUrl` at wherever that API runs. On a physical device you MUST use the
 * machine's LAN IP (e.g. `http://192.168.1.20:38340`), not `localhost` — `localhost` on a
 * phone means the phone. The default is read from `app.json > expo.extra.apiBaseUrl` and can
 * be overridden at runtime under "Advanced" on the login screen.
 */
const extra = (Constants.expoConfig?.extra ?? {}) as { apiBaseUrl?: string };

export const config = {
  /** Base URL of the auth API. Overridable at runtime from the login screen. */
  defaultApiBaseUrl: extra.apiBaseUrl ?? 'http://localhost:38340',

  endpoints: {
    /** Step 1 — validate credentials, return the member envelope. */
    login: '/api/v1/auth/login',
    /** Step 2 — exchange the member envelope for a JWT `securityToken`. */
    completeLogin: '/api/v1/auth/completelogin',
    /** Federated SSO — GET ?lob=&ssoName=&planCode= (Bearer securityToken) -> ssoUrl. */
    sso: '/api/v1/sso',
  },

  /**
   * Constants the login envelope carries (tenant / channel identifiers). Taken from the
   * working Postman requests. Adjust per environment if needed.
   */
  auth: {
    appId: 'LAEX',
    planId: 'LAEX',
    entity: 2,
    lang: 'en',
    version: '2',
  },

  /**
   * SSO portal catalog shown after login. The API has no "list SSOs" endpoint, so the
   * launchable portals are declared here. Each entry's `ssoName` + `lob` (+ `planCode`) are
   * sent to `GET /api/v1/sso`. Valid SSO names come from the API's SSO config (e.g. HRA,
   * SoftheonSSO, CHATSSO, CERTIFISSO). HRA is the worked example from the requirements.
   */
  ssoPortals: [
    {
      ssoName: 'HRA',
      lob: 'LAEX',
      planCode: 'LAEX',
      name: 'HRA Portal',
      description: 'Health Reimbursement Account — balances, claims & reimbursements.',
      accent: '#34D399',
    },
    {
      ssoName: 'PLANOFCARESSO',
      lob: 'LAEX',
      planCode: 'LAEX',
      name: 'Plan of Care',
      description: 'Care plan, assessments and care-team details (SSO).',
      accent: '#60A5FA',
    },
    {
      ssoName: 'ABARCASSO',
      lob: 'LAEX',
      planCode: 'LAEX',
      name: 'Abarca — Pharmacy',
      description: 'Pharmacy benefits, prescriptions and claims (SSO).',
      accent: '#A78BFA',
    },
  ] as SsoPortal[],

  /**
   * When an SSO WebView navigates to a URL that starts with any of these, the app treats
   * the portal login as complete and closes the WebView. Empty = never auto-close (tap Done).
   */
  ssoSuccessUrlPrefixes: [] as string[],
};
