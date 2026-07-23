import { config } from '../config';
import { request } from './http';
import type { SsoPortal, SsoResponse } from './types';

/**
 * Resolve federated sign-on for a portal: `GET /api/v1/sso?lob=&ssoName=&planCode=`.
 *
 * Authenticated with the JWT `securityToken` from completelogin. Member identity is read by
 * the API from that token, so nothing about *who* is signing on is sent. The response's
 * `ssoUrl` is the complete PingFederate URL (`startSSO.ping?...&opentoken=...`) to open in a
 * WebView; it may be null for a skipped LOB or an SSO name with no URL provider. A 404 means
 * the lob/ssoName pairing isn't configured.
 */
export function getSso(
  baseUrl: string,
  securityToken: string,
  portal: SsoPortal,
): Promise<SsoResponse> {
  const params = new URLSearchParams({ lob: portal.lob, ssoName: portal.ssoName });
  if (portal.planCode) params.set('planCode', portal.planCode);

  return request<SsoResponse>(baseUrl, `${config.endpoints.sso}?${params.toString()}`, {
    accessToken: securityToken,
  });
}
