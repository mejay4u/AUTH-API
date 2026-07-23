import { config } from '../config';
import {
  deepFindAny,
  deepFindFirstString,
  deepFindString,
  topLevelKeys,
} from './extract';
import { ApiError, request } from './http';
import type {
  CompleteLoginRequest,
  CompleteLoginResponse,
  InitiateLoginRequest,
  MemberEnvelope,
} from './types';

/** Result of a completed sign-in: the JWT plus the raw completelogin response. */
export interface SignInResult {
  securityToken: string;
  raw: CompleteLoginResponse;
}

/** Thrown when the flow finishes but no token is found — carries raw responses for on-screen debug. */
export class SignInError extends ApiError {
  debug: string;
  constructor(message: string, debug: string) {
    super(message, 502);
    this.name = 'SignInError';
    this.debug = debug;
  }
}

function dump(label: string, value: unknown): string {
  let json: string;
  try {
    json = JSON.stringify(value, null, 1);
  } catch {
    json = String(value);
  }
  if (json.length > 1600) json = `${json.slice(0, 1600)}… (truncated)`;
  return `${label}:\n${json}`;
}

/**
 * Step 1 — validate the member's credentials. Returns the member envelope (TransId, member
 * id, profile) that step 2 consumes.
 */
export function initiateLogin(
  baseUrl: string,
  userId: string,
  password: string,
): Promise<MemberEnvelope> {
  const body: InitiateLoginRequest = {
    TransId: '',
    UserId: userId,
    Password: password,
    AppId: config.auth.appId,
    PlanId: config.auth.planId,
    Entity: config.auth.entity,
    Lang: config.auth.lang,
    Version: config.auth.version,
    SessionKey: '',
  };
  return request<MemberEnvelope>(baseUrl, config.endpoints.login, {
    method: 'POST',
    body,
  });
}

/**
 * Step 2 — exchange the member profile for a JWT `securityToken`.
 *
 * Two things this endpoint requires (both learned from the API's Scalar docs / Postman):
 *   1. `Authorization: Bearer <accessToken>` where the token is the one returned by `login`.
 *      Without it the endpoint returns a degraded passthrough shape with no `securityToken`.
 *   2. A clean member envelope body (matching the working Postman request), NOT the raw login
 *      response forwarded wholesale.
 *
 * Each field is pulled from the login response wherever it sits (the profile may be nested
 * under a `data`/`loginIdentity` wrapper), so extraction is by key.
 */
export function completeLogin(
  baseUrl: string,
  login: MemberEnvelope,
): Promise<CompleteLoginResponse> {
  const loginAccessToken = deepFindFirstString(login, [
    'accessToken',
    'securityToken',
    'token',
  ]);
  const phones = deepFindAny(login, 'phoneNumbersList');
  const body: CompleteLoginRequest = {
    TransId: deepFindString(login, 'transId') ?? '',
    AppId: deepFindString(login, 'appId') ?? config.auth.appId,
    Entity: (deepFindAny(login, 'entity') as number | undefined) ?? config.auth.entity,
    Lang: deepFindString(login, 'lang') ?? config.auth.lang,
    Version: deepFindString(login, 'version') ?? config.auth.version,
    EligibilityStatus: deepFindString(login, 'eligibilityStatus') ?? '',
    FamilyLinkId: deepFindString(login, 'familyLinkId') ?? '',
    MemberId: deepFindString(login, 'memberId') ?? '',
    MedicaidId: deepFindString(login, 'medicaidId') ?? '',
    FirstName: deepFindString(login, 'firstName') ?? '',
    LastName: deepFindString(login, 'lastName') ?? '',
    DateOfBirth: deepFindString(login, 'dateOfBirth') ?? '',
    UserName: deepFindString(login, 'userName') ?? '',
    EmailId: deepFindFirstString(login, ['emailId', 'memberEmailId']) ?? '',
    PhoneNumbersList: Array.isArray(phones) ? (phones as string[]) : [],
  };
  return request<CompleteLoginResponse>(baseUrl, config.endpoints.completeLogin, {
    method: 'POST',
    body,
    accessToken: loginAccessToken,
  });
}

/**
 * Full sign-in: validate credentials, then complete the login to obtain the JWT. Throws an
 * ApiError if either step fails or no security token comes back.
 */
export async function signInFlow(
  baseUrl: string,
  userId: string,
  password: string,
): Promise<SignInResult> {
  const envelope = await initiateLogin(baseUrl, userId, password);
  const loginToken = deepFindFirstString(envelope, ['accessToken', 'securityToken', 'token']);
  const completed = await completeLogin(baseUrl, envelope);

  // The JWT should come back on completelogin (as `securityToken`, or `accessToken` on some
  // builds). If neither is present with a value, fall back to the login token if it had one.
  const securityToken =
    deepFindFirstString(completed, ['securityToken', 'accessToken', 'token']) ?? loginToken;

  if (!securityToken) {
    const debug = [
      `loginTokenPresent: ${Boolean(loginToken)}`,
      `completeFields: ${topLevelKeys(completed).join(', ') || '(none)'}`,
      dump('LOGIN', envelope),
      dump('COMPLETELOGIN', completed),
    ].join('\n\n');
    throw new SignInError('No token found. Raw responses below.', debug);
  }
  return { securityToken, raw: completed };
}
