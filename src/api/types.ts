/** Shapes exchanged with the Auth API. */

/** Response from POST /api/v1/auth/login and /refresh (AuthResponse in the .NET API). */
export interface AuthResponse {
  memberId: string;
  username: string;
  tokenType: string;
  accessToken: string;
  accessTokenExpiresUtc: string;
  refreshToken: string;
  refreshTokenExpiresUtc: string;
  lobs: string[];
  planIds: number[];
}

/** Response from GET /api/v1/members/me. */
export interface MeResponse {
  memberId: string | null;
  username: string | null;
  email: string | null;
  firstName: string | null;
  lastName: string | null;
  lobs: string[];
  planIds: number[];
}

/** A single-sign-on target shown to the member after login. */
export interface Portal {
  code: string;
  name: string;
  description?: string;
  accent?: string;
}

/**
 * Descriptor the app uses to launch a portal in a WebView. Returned by `ssoInitiate`.
 * Supports the two common SSO binding styles:
 *
 *   1. Redirect / GET  — `{ method: "GET", url }`. The app just loads `url`.
 *   2. SAML / form POST — `{ method: "POST", url, formFields }`. The app renders an
 *      auto-submitting HTML form that POSTs `formFields` to `url`.
 */
export interface SsoLaunch {
  portal: string;
  method?: 'GET' | 'POST';
  /** The portal / ACS URL to open (GET) or POST the form to. */
  url: string;
  /** Hidden form fields for POST binding, e.g. { SAMLResponse, RelayState }. */
  formFields?: Record<string, string>;
}

/** RFC 7807 ProblemDetails returned by the API on errors. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
}
