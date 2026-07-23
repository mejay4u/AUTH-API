import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react';

import { signInFlow } from '../api/auth';
import { ApiError } from '../api/http';
import type { CompleteLoginResponse } from '../api/types';
import { config } from '../config';
import {
  StoredSession,
  clearSession,
  loadSession,
  saveSession,
} from './storage';

interface AuthState {
  /** True while restoring the persisted session on startup. */
  loading: boolean;
  session: StoredSession | null;
  baseUrl: string;
  setBaseUrl: (url: string) => void;
  /** Runs login -> completelogin and persists the resulting security token. */
  signIn: (userId: string, password: string) => Promise<void>;
  signOut: () => Promise<void>;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

function toStr(v: unknown): string | null {
  return typeof v === 'string' && v.length > 0 ? v : null;
}

function sessionFrom(baseUrl: string, r: CompleteLoginResponse): StoredSession {
  return {
    baseUrl,
    securityToken: r.securityToken as string,
    memberId: toStr(r.memberId) ?? '',
    userName: toStr(r.userName) ?? '',
    firstName: toStr(r.firstName),
    lastName: toStr(r.lastName),
    email: toStr(r.emailId),
    role: toStr(r.memberRole),
  };
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
    async (userId: string, password: string) => {
      const completed = await signInFlow(baseUrl, userId, password);
      await persist(sessionFrom(baseUrl, completed));
    },
    [baseUrl, persist],
  );

  const signOut = useCallback(async () => {
    await persist(null);
  }, [persist]);

  const value = useMemo<AuthState>(
    () => ({ loading, session, baseUrl, setBaseUrl, signIn, signOut }),
    [loading, session, baseUrl, signIn, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>.');
  return ctx;
}

export { ApiError };
