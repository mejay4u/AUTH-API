import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react';

import { login as apiLogin, refresh as apiRefresh } from '../api/auth';
import { ApiError } from '../api/http';
import type { AuthResponse } from '../api/types';
import { config } from '../config';
import {
  StoredSession,
  clearSession,
  loadSession,
  saveSession,
} from './storage';

interface AuthState {
  /** Null while restoring the persisted session on startup. */
  loading: boolean;
  session: StoredSession | null;
  baseUrl: string;
  setBaseUrl: (url: string) => void;
  signIn: (username: string, password: string, lob: string) => Promise<void>;
  signOut: () => Promise<void>;
  /** Returns a valid access token, refreshing first if it is expired/near expiry. */
  getValidAccessToken: () => Promise<string>;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

function sessionFromResponse(baseUrl: string, r: AuthResponse): StoredSession {
  return {
    baseUrl,
    memberId: r.memberId,
    username: r.username,
    accessToken: r.accessToken,
    accessTokenExpiresUtc: r.accessTokenExpiresUtc,
    refreshToken: r.refreshToken,
    refreshTokenExpiresUtc: r.refreshTokenExpiresUtc,
    lobs: r.lobs,
    planIds: r.planIds,
  };
}

function isExpiringSoon(iso: string, skewSeconds = 30): boolean {
  const expiry = Date.parse(iso);
  if (Number.isNaN(expiry)) return true;
  return expiry - Date.now() <= skewSeconds * 1000;
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [loading, setLoading] = useState(true);
  const [session, setSession] = useState<StoredSession | null>(null);
  const [baseUrl, setBaseUrl] = useState(config.defaultApiBaseUrl);

  useEffect(() => {
    (async () => {
      const restored = await loadSession();
      if (restored) {
        setSession(restored);
        setBaseUrl(restored.baseUrl);
      }
      setLoading(false);
    })();
  }, []);

  const persist = useCallback(async (next: StoredSession | null) => {
    setSession(next);
    if (next) await saveSession(next);
    else await clearSession();
  }, []);

  const signIn = useCallback(
    async (username: string, password: string, lob: string) => {
      const res = await apiLogin(baseUrl, username, password, lob);
      await persist(sessionFromResponse(baseUrl, res));
    },
    [baseUrl, persist],
  );

  const signOut = useCallback(async () => {
    await persist(null);
  }, [persist]);

  const getValidAccessToken = useCallback(async (): Promise<string> => {
    if (!session) throw new ApiError('Not signed in.', 401);
    if (!isExpiringSoon(session.accessTokenExpiresUtc)) {
      return session.accessToken;
    }
    // Access token expired/expiring — rotate via the refresh token.
    try {
      const res = await apiRefresh(session.baseUrl, session.refreshToken);
      const next = sessionFromResponse(session.baseUrl, res);
      await persist(next);
      return next.accessToken;
    } catch (err) {
      // Refresh failed (expired/reused) — force a fresh login.
      await persist(null);
      throw err instanceof ApiError
        ? err
        : new ApiError('Your session has expired. Please sign in again.', 401);
    }
  }, [session, persist]);

  const value = useMemo<AuthState>(
    () => ({
      loading,
      session,
      baseUrl,
      setBaseUrl,
      signIn,
      signOut,
      getValidAccessToken,
    }),
    [loading, session, baseUrl, signIn, signOut, getValidAccessToken],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>.');
  return ctx;
}
