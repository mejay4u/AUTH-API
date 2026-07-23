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

/**
 * A single-sign-on target shown to the member after login. This is app-side catalog
 * config (the Auth API has no "list SSOs" endpoint): `ssoName` + `lob` are what get sent
 * to `GET /api/v1/sso`; the rest is presentation.
 */
export interface SsoPortal {
  /** SSO name understood by the API, e.g. "HRA", "CHATSSO", "CERTIFISSO". */
  ssoName: string;
  /** Line-of-business code the SSO config is keyed by, e.g. "2100". */
  lob: string;
  /** Optional plan code, when the SSO config is plan-specific. */
  planCode?: string;
  name: string;
  description?: string;
  accent?: string;
}

/** One SSO configuration row (SsoConfigItem in the .NET API). */
export interface SsoConfigItem {
  ssoName: string;
  description: string | null;
  pingFedUrl: string | null;
  pingFedReturnUrl: string | null;
  assessmentName: string | null;
  level: string | null;
  argusCustomerId: string | null;
}

/**
 * Response from `GET /api/v1/sso` (SsoResponse in the .NET API). `ssoUrl` is the complete
 * federated sign-on URL to open in a WebView. It can be null for a skipped LOB or an SSO
 * name with no URL provider, in which case only configuration is returned.
 */
export interface SsoResponse {
  memberId: string;
  designeeId: string | null;
  role: string;
  lob: string;
  ssoName: string;
  assessmentName: string | null;
  ssoUrl: string | null;
  data: SsoConfigItem[];
}

/** RFC 7807 ProblemDetails returned by the API on errors. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
}
