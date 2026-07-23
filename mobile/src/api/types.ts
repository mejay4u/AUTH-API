/** Shapes exchanged with the member portal auth API. */

/**
 * Request body for step 1, `POST /api/v1/auth/login`. Field names mirror the working
 * Postman request. `TransId` and `SessionKey` are sent empty — the server populates them.
 */
export interface InitiateLoginRequest {
  TransId: string;
  UserId: string;
  AppId: string;
  Password: string;
  Entity: number;
  Lang: string;
  Version: string;
  PlanId: string;
  SessionKey: string;
}

/**
 * The member envelope. Returned by `/auth/login` and posted back to `/auth/completelogin`
 * (ASP.NET model binding is case-insensitive, so the camelCase login response binds fine as
 * the completelogin body). Typed loosely with the known fields plus a passthrough index so
 * the whole envelope can be forwarded without dropping anything the server added.
 */
export interface MemberEnvelope {
  transId?: string | null;
  appId?: string | null;
  planId?: string | null;
  entity?: number | null;
  lang?: string | null;
  version?: string | null;
  memberId?: string | null;
  medicaidId?: string | null;
  altId?: string | null;
  familyLinkId?: string | null;
  eligibilityStatus?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  dateOfBirth?: string | null;
  userName?: string | null;
  emailId?: string | null;
  phoneNumbersList?: string[] | null;
  status?: string | null;
  messageStatus?: string | null;
  code?: string | null;
  errors?: unknown[] | null;
  isTestUser?: boolean;
  isTwoFactorAuth?: boolean;
  [key: string]: unknown;
}

/**
 * Response from step 2, `POST /api/v1/auth/completelogin`. Carries the JWT `securityToken`
 * used as the Bearer token for `GET /api/v1/sso`, plus display fields for the home screen.
 */
export interface CompleteLoginResponse extends MemberEnvelope {
  securityToken?: string | null;
  isLoggedInMember?: boolean;
  isInternalUser?: boolean;
  isTermedOrDelinquentMember?: boolean;
  mailType?: string | null;
  memberRole?: string | null;
  gender?: string | null;
  languagePreference?: string | null;
  securityQuestion?: string | null;
}

/**
 * An SSO target shown after login. App-side catalog config (no "list SSOs" endpoint):
 * `ssoName` + `lob` (+ optional `planCode`) are sent to `GET /api/v1/sso`.
 */
export interface SsoPortal {
  ssoName: string;
  lob: string;
  planCode?: string;
  name: string;
  description?: string;
  accent?: string;
}

/** One SSO configuration row (the `data[]` items in the SSO response). */
export interface SsoConfigItem {
  ssoName: string;
  description: string | null;
  pingFedUrl: string | null;
  pingFedReturnUrl: string | null;
  assessmentName: string | null;
  keyPath?: string | null;
}

/**
 * Response from `GET /api/v1/sso`. `ssoUrl` is the complete PingFederate sign-on URL
 * (`startSSO.ping?...&opentoken=...`) to open in a WebView. It can be null for a skipped LOB
 * or an SSO name with no URL provider.
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
