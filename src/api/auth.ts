import { config } from '../config';
import { ApiError, request } from './http';
import type {
  CompleteLoginResponse,
  InitiateLoginRequest,
  MemberEnvelope,
} from './types';

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
 * Step 2 — exchange the member envelope for a JWT `securityToken`. The envelope returned by
 * `initiateLogin` is forwarded as-is (with the tenant constants ensured), so no field the
 * server added is dropped.
 */
export function completeLogin(
  baseUrl: string,
  envelope: MemberEnvelope,
): Promise<CompleteLoginResponse> {
  const body: MemberEnvelope = {
    ...envelope,
    appId: envelope.appId ?? config.auth.appId,
    planId: envelope.planId ?? config.auth.planId,
    entity: envelope.entity ?? config.auth.entity,
    lang: envelope.lang ?? config.auth.lang,
    version: envelope.version ?? config.auth.version,
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
): Promise<CompleteLoginResponse> {
  const envelope = await initiateLogin(baseUrl, userId, password);
  const completed = await completeLogin(baseUrl, envelope);
  if (!completed.securityToken) {
    throw new ApiError('Login completed but no security token was returned.', 502);
  }
  return completed;
}
