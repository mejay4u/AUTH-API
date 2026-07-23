import Constants from 'expo-constants';

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

  /** Auth API routes (these already exist in the .NET solution). */
  endpoints: {
    login: '/api/v1/auth/login',
    refresh: '/api/v1/auth/refresh',
    me: '/api/v1/members/me',

    /**
     * SSO endpoints. Adjust these to match your Auth API.
     *
     * - `ssoInitiate`   : POST { portal } (Bearer) -> launch descriptor (see SsoLaunch type).
     *                     `{portal}` in the path is replaced with the portal code, e.g. "HRA".
     * - `ssoCompleteLogon` : optional POST the app calls once the portal round-trip is done.
     * - `ssoPortals`    : optional GET returning the portals available to the member.
     */
    ssoInitiate: '/api/v1/sso/{portal}/initiate',
    ssoCompleteLogon: '/api/v1/sso/{portal}/complete-logon',
    ssoPortals: '/api/v1/sso/portals',
  },

  /**
   * Lines of business the login screen lets the member pick from. `login` requires a
   * `lob` — it selects which line-of-business database to authenticate against.
   */
  lobs: ['DENTAL', 'VISION', 'MEDICAL', 'RX'] as const,

  /**
   * Fallback SSO portal catalog shown after login when the API does not expose a
   * `ssoPortals` endpoint (or it returns nothing). `code` is what gets sent to
   * `ssoInitiate`. HRA is included as the worked example from the requirements.
   */
  fallbackPortals: [
    {
      code: 'HRA',
      name: 'HRA Portal',
      description: 'Health Reimbursement Account — balances, claims & reimbursements.',
      accent: '#34D399',
    },
    {
      code: 'DENTAL',
      name: 'Dental Benefits',
      description: 'View dental coverage, find a dentist, track claims.',
      accent: '#60A5FA',
    },
    {
      code: 'VISION',
      name: 'Vision Benefits',
      description: 'Vision plan details, in-network providers and allowances.',
      accent: '#A78BFA',
    },
  ],

  /**
   * When an SSO WebView navigates to a URL that starts with any of these, the app treats
   * the portal login as complete and closes the WebView. Tune to your portals' post-login
   * landing pages. Empty array = never auto-close (user taps Done).
   */
  ssoSuccessUrlPrefixes: [] as string[],
};

export type LobCode = (typeof config.lobs)[number];
