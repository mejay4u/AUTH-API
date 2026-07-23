import { config } from '../config';
import { request } from './http';
import type { AuthResponse, MeResponse } from './types';

export function login(
  baseUrl: string,
  username: string,
  password: string,
  lob: string,
): Promise<AuthResponse> {
  return request<AuthResponse>(baseUrl, config.endpoints.login, {
    method: 'POST',
    body: { username, password, lob },
  });
}

export function refresh(baseUrl: string, refreshToken: string): Promise<AuthResponse> {
  return request<AuthResponse>(baseUrl, config.endpoints.refresh, {
    method: 'POST',
    body: { refreshToken },
  });
}

export function getMe(baseUrl: string, accessToken: string): Promise<MeResponse> {
  return request<MeResponse>(baseUrl, config.endpoints.me, { accessToken });
}
