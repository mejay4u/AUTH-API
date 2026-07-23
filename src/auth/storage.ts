import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

/**
 * Persisted session. Tokens are kept in the device keychain/keystore via
 * expo-secure-store on native. On web (no SecureStore) we fall back to localStorage,
 * which is fine for local development in a browser.
 */
export interface StoredSession {
  baseUrl: string;
  memberId: string;
  username: string;
  accessToken: string;
  accessTokenExpiresUtc: string;
  refreshToken: string;
  refreshTokenExpiresUtc: string;
  lobs: string[];
  planIds: number[];
}

const KEY = 'member_portal_session_v1';

const webAvailable = Platform.OS === 'web' && typeof localStorage !== 'undefined';

export async function saveSession(session: StoredSession): Promise<void> {
  const value = JSON.stringify(session);
  if (webAvailable) {
    localStorage.setItem(KEY, value);
    return;
  }
  await SecureStore.setItemAsync(KEY, value);
}

export async function loadSession(): Promise<StoredSession | null> {
  const raw = webAvailable
    ? localStorage.getItem(KEY)
    : await SecureStore.getItemAsync(KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as StoredSession;
  } catch {
    return null;
  }
}

export async function clearSession(): Promise<void> {
  if (webAvailable) {
    localStorage.removeItem(KEY);
    return;
  }
  await SecureStore.deleteItemAsync(KEY);
}
