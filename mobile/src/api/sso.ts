import { config } from '../config';
import { ApiError, request } from './http';
import type { Portal, SsoLaunch } from './types';

function pathFor(template: string, portalCode: string): string {
  return template.replace('{portal}', encodeURIComponent(portalCode));
}

/**
 * Fetch the SSO portals available to the signed-in member. If the API doesn't expose
 * the `ssoPortals` endpoint (404), fall back to the configured catalog so the member
 * still sees launchable links (e.g. HRA).
 */
export async function getPortals(baseUrl: string, accessToken: string): Promise<Portal[]> {
  try {
    const portals = await request<Portal[]>(baseUrl, config.endpoints.ssoPortals, {
      accessToken,
    });
    if (Array.isArray(portals) && portals.length > 0) return portals;
  } catch (err) {
    if (!(err instanceof ApiError) || err.status !== 404) {
      // A real error (network, 401, 500) — surface it.
      if (err instanceof ApiError && err.status === 401) throw err;
    }
    // 404 / empty / not implemented -> fall through to the catalog.
  }
  return config.fallbackPortals;
}

/**
 * Begin SSO ("initiate login") for a portal. Returns a launch descriptor telling the
 * app how to open the portal (GET redirect or SAML form POST).
 */
export function initiateSso(
  baseUrl: string,
  accessToken: string,
  portalCode: string,
): Promise<SsoLaunch> {
  return request<SsoLaunch>(baseUrl, pathFor(config.endpoints.ssoInitiate, portalCode), {
    method: 'POST',
    accessToken,
    body: { portal: portalCode },
  });
}

/**
 * Optional "complete logon" call once the portal round-trip has finished. Best-effort:
 * a 404 (endpoint not implemented) is ignored so it never blocks the user.
 */
export async function completeLogon(
  baseUrl: string,
  accessToken: string,
  portalCode: string,
): Promise<void> {
  try {
    await request<unknown>(baseUrl, pathFor(config.endpoints.ssoCompleteLogon, portalCode), {
      method: 'POST',
      accessToken,
      body: { portal: portalCode },
    });
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) return;
    throw err;
  }
}
