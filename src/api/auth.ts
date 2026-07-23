import { config } from '../config';
import {
  deepFindAny,
  deepFindFirstString,
  deepFindString,
  topLevelKeys,
} from './extract';
import { ApiError, request } from './http';
import type {
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
 * Step 2 — exchange the member profile for the JWT. Per the API's actual behaviour:
 *   - `login` returns NO token; `completelogin` returns the `accessToken`.
 *   - `completelogin` does NOT require a Bearer token.
 *
 * The request body must carry EVERY field the endpoint expects (mirroring the working
 * Scalar/Postman request), most importantly `SessionKey` and `LoginIdentity` which correlate
 * the transaction — omitting them makes the server return the response with a null token.
 * Each field is extracted from the login response by key (deepFind, so a nested `data`
 * wrapper is handled) and emitted with the exact PascalCase the endpoint uses.
 */
export function completeLogin(
  baseUrl: string,
  login: MemberEnvelope,
): Promise<CompleteLoginResponse> {
  const str = (key: string) => deepFindString(login, key);
  const firstStr = (keys: string[]) => deepFindFirstString(login, keys);
  const boolOf = (key: string): boolean => {
    const v = deepFindAny(login, key);
    return typeof v === 'boolean' ? v : false;
  };
  const numOf = (key: string): number | undefined => {
    const v = deepFindAny(login, key);
    return typeof v === 'number' ? v : undefined;
  };
  const arrOf = (key: string): unknown[] => {
    const v = deepFindAny(login, key);
    return Array.isArray(v) ? v : [];
  };
  const nullableStr = (key: string): string | null => str(key);

  const body = {
    TransId: str('transId') ?? '',
    AppId: str('appId') ?? config.auth.appId,
    Entity: numOf('entity') ?? config.auth.entity,
    Lang: str('lang') ?? config.auth.lang,
    Version: str('version') ?? config.auth.version,
    EligibilityStatus: str('eligibilityStatus') ?? '',
    FamilyLinkId: str('familyLinkId') ?? '',
    MemberId: str('memberId') ?? '',
    MedicaidId: str('medicaidId') ?? '',
    FirstName: str('firstName') ?? '',
    LastName: str('lastName') ?? '',
    DateOfBirth: str('dateOfBirth') ?? '',
    UserName: str('userName') ?? '',
    EmailId: firstStr(['emailId', 'memberEmailId']) ?? '',
    PhoneNumbersList: arrOf('phoneNumbersList'),
    IsLoggedInMember: boolOf('isLoggedInMember'),
    IsTermedOrDelinquentMember: boolOf('isTermedOrDelinquentMember'),
    Gender: nullableStr('gender'),
    LanguagePreference: nullableStr('languagePreference'),
    MemberRole: firstStr(['memberRole', 'userRole']),
    IsTermedMember: boolOf('isTermedMember'),
    IsTermedReadOnlyMember: boolOf('isTermedReadOnlyMember'),
    IsInactiveReadOnlyMember: boolOf('isInactiveReadOnlyMember'),
    EligibilityCoverageStatus: nullableStr('eligibilityCoverageStatus'),
    EligibilityCoverageDate: nullableStr('eligibilityCoverageDate'),
    MiddleName: nullableStr('middleName'),
    MedicareId: nullableStr('medicareId'),
    MemberDependentsData: arrOf('memberDependentsData'),
    MFAPreference: boolOf('mfaPreference'),
    // The two fields that were missing — they tie completelogin back to the login transaction.
    SessionKey: str('sessionKey') ?? '',
    LoginIdentity: str('loginIdentity') ?? '',
    IsCustomUserId: boolOf('isCustomUserId'),
  };

  return request<CompleteLoginResponse>(baseUrl, config.endpoints.completeLogin, {
    method: 'POST',
    body,
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
  const completed = await completeLogin(baseUrl, envelope);

  // completelogin returns the JWT as `accessToken` (securityToken accepted as a fallback).
  const securityToken = deepFindFirstString(completed, [
    'accessToken',
    'securityToken',
    'token',
  ]);

  if (!securityToken) {
    const debug = [
      `completeFields: ${topLevelKeys(completed).join(', ') || '(none)'}`,
      dump('LOGIN', envelope),
      dump('COMPLETELOGIN', completed),
    ].join('\n\n');
    throw new SignInError('No token found. Raw responses below.', debug);
  }
  return { securityToken, raw: completed };
}
