import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import React, { useCallback, useEffect, useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  RefreshControl,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { getMe } from '../api/auth';
import { ApiError } from '../api/http';
import { getPortals, initiateSso } from '../api/sso';
import type { MeResponse, Portal } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { Button } from '../components/Button';
import { PortalCard } from '../components/PortalCard';
import type { RootStackParamList } from '../navigation/types';
import { theme } from '../theme';

type Props = NativeStackScreenProps<RootStackParamList, 'Home'>;

export function HomeScreen({ navigation }: Props) {
  const { session, signOut, getValidAccessToken } = useAuth();

  const [me, setMe] = useState<MeResponse | null>(null);
  const [portals, setPortals] = useState<Portal[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [launching, setLaunching] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!session) return;
    setError(null);
    try {
      const token = await getValidAccessToken();
      const [meRes, portalRes] = await Promise.all([
        getMe(session.baseUrl, token),
        getPortals(session.baseUrl, token),
      ]);
      setMe(meRes);
      setPortals(portalRes);
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        // Session invalid — AuthContext has already cleared it; navigator will redirect.
        return;
      }
      setError(err instanceof ApiError ? err.message : 'Failed to load your portals.');
    }
  }, [session, getValidAccessToken]);

  useEffect(() => {
    (async () => {
      setLoading(true);
      await load();
      setLoading(false);
    })();
  }, [load]);

  const onRefresh = useCallback(async () => {
    setRefreshing(true);
    await load();
    setRefreshing(false);
  }, [load]);

  const onLaunch = useCallback(
    async (portal: Portal) => {
      if (!session) return;
      setLaunching(portal.code);
      try {
        const token = await getValidAccessToken();
        const launch = await initiateSso(session.baseUrl, token, portal.code);
        if (!launch?.url) {
          throw new ApiError('The SSO service did not return a launch URL.', 502);
        }
        navigation.navigate('SsoWebView', {
          portalCode: portal.code,
          portalName: portal.name,
          launch,
        });
      } catch (err) {
        const message =
          err instanceof ApiError ? err.message : 'Could not start single sign-on.';
        Alert.alert(`${portal.name} sign-on`, message);
      } finally {
        setLaunching(null);
      }
    },
    [session, getValidAccessToken, navigation],
  );

  const displayName =
    [me?.firstName, me?.lastName].filter(Boolean).join(' ') ||
    me?.username ||
    session?.username ||
    'Member';

  if (loading) {
    return (
      <View style={styles.center}>
        <ActivityIndicator color={theme.colors.primary} size="large" />
      </View>
    );
  }

  return (
    <SafeAreaView style={styles.safe} edges={['bottom']}>
      <ScrollView
        contentContainerStyle={styles.content}
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={onRefresh}
            tintColor={theme.colors.textMuted}
          />
        }
      >
        <View style={styles.greeting}>
          <Text style={styles.hello}>Welcome back,</Text>
          <Text style={styles.name}>{displayName}</Text>
          {me?.email ? <Text style={styles.email}>{me.email}</Text> : null}
        </View>

        {me?.lobs?.length ? (
          <View style={styles.lobBar}>
            {me.lobs.map((l) => (
              <View key={l} style={styles.lobPill}>
                <Text style={styles.lobPillText}>{l}</Text>
              </View>
            ))}
          </View>
        ) : null}

        <Text style={styles.sectionTitle}>Single sign-on</Text>
        <Text style={styles.sectionHint}>
          Tap a portal to sign in — you're logged in there automatically, no second password.
        </Text>

        {error ? <Text style={styles.error}>{error}</Text> : null}

        {portals.length === 0 && !error ? (
          <Text style={styles.empty}>No SSO portals are available for your account.</Text>
        ) : (
          portals.map((p) => (
            <PortalCard
              key={p.code}
              portal={p}
              onPress={onLaunch}
              loading={launching === p.code}
            />
          ))
        )}

        <Button
          title="Sign out"
          variant="ghost"
          onPress={signOut}
          style={{ marginTop: theme.spacing(3) }}
        />
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: theme.colors.background },
  center: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: theme.colors.background,
  },
  content: { padding: theme.spacing(3) },
  greeting: { marginBottom: theme.spacing(2) },
  hello: { color: theme.colors.textMuted, fontSize: 15 },
  name: {
    color: theme.colors.text,
    fontSize: 28,
    fontWeight: '800',
    letterSpacing: -0.5,
    marginTop: 2,
  },
  email: { color: theme.colors.textMuted, fontSize: 14, marginTop: 2 },
  lobBar: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: theme.spacing(3) },
  lobPill: {
    paddingVertical: 5,
    paddingHorizontal: 12,
    borderRadius: 999,
    backgroundColor: theme.colors.surfaceAlt,
    borderWidth: 1,
    borderColor: theme.colors.border,
  },
  lobPillText: { color: theme.colors.text, fontSize: 12, fontWeight: '700' },
  sectionTitle: {
    color: theme.colors.text,
    fontSize: 18,
    fontWeight: '700',
    marginBottom: 4,
  },
  sectionHint: {
    color: theme.colors.textMuted,
    fontSize: 14,
    marginBottom: theme.spacing(2),
    lineHeight: 20,
  },
  error: { color: theme.colors.danger, marginBottom: theme.spacing(1.5), fontSize: 14 },
  empty: { color: theme.colors.textMuted, fontSize: 14, marginBottom: theme.spacing(2) },
});
