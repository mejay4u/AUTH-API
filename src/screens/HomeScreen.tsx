import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import React, { useCallback, useState } from 'react';
import { Alert, ScrollView, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ApiError } from '../api/http';
import { getSso } from '../api/sso';
import type { SsoPortal } from '../api/types';
import { useAuth } from '../auth/AuthContext';
import { Button } from '../components/Button';
import { PortalCard } from '../components/PortalCard';
import { config } from '../config';
import type { RootStackParamList } from '../navigation/types';
import { theme } from '../theme';

type Props = NativeStackScreenProps<RootStackParamList, 'Home'>;

export function HomeScreen({ navigation }: Props) {
  const { session, signOut } = useAuth();
  const [launching, setLaunching] = useState<string | null>(null);

  const portals = config.ssoPortals;

  const onLaunch = useCallback(
    async (portal: SsoPortal) => {
      if (!session) return;
      const key = `${portal.lob}:${portal.ssoName}`;
      setLaunching(key);
      try {
        const res = await getSso(session.baseUrl, session.securityToken, portal);
        if (!res.ssoUrl) {
          Alert.alert(
            portal.name,
            'Single sign-on is not available for this portal on your account right now.',
          );
          return;
        }
        navigation.navigate('SsoWebView', { portalName: portal.name, url: res.ssoUrl });
      } catch (err) {
        if (err instanceof ApiError && err.status === 401) {
          Alert.alert('Session expired', 'Please sign in again.', [
            { text: 'OK', onPress: () => void signOut() },
          ]);
          return;
        }
        const message =
          err instanceof ApiError && err.status === 404
            ? 'This portal is not configured for your line of business.'
            : err instanceof ApiError
              ? err.message
              : 'Could not start single sign-on.';
        Alert.alert(`${portal.name} sign-on`, message);
      } finally {
        setLaunching(null);
      }
    },
    [session, navigation, signOut],
  );

  const displayName =
    [session?.firstName, session?.lastName].filter(Boolean).join(' ') ||
    session?.userName ||
    'Member';

  return (
    <SafeAreaView style={styles.safe} edges={['bottom']}>
      <ScrollView contentContainerStyle={styles.content}>
        <View style={styles.greeting}>
          <Text style={styles.hello}>Welcome back,</Text>
          <Text style={styles.name}>{displayName}</Text>
          {session?.email ? <Text style={styles.email}>{session.email}</Text> : null}
        </View>

        <View style={styles.metaBar}>
          {session?.memberId ? (
            <View style={styles.metaPill}>
              <Text style={styles.metaPillText}>ID {session.memberId}</Text>
            </View>
          ) : null}
          {session?.role ? (
            <View style={styles.metaPill}>
              <Text style={styles.metaPillText}>{session.role}</Text>
            </View>
          ) : null}
        </View>

        <Text style={styles.sectionTitle}>Single sign-on</Text>
        <Text style={styles.sectionHint}>
          Tap a portal to sign in — you're logged in there automatically, no second password.
        </Text>

        {portals.length === 0 ? (
          <Text style={styles.empty}>No SSO portals are configured.</Text>
        ) : (
          portals.map((p) => (
            <PortalCard
              key={`${p.lob}:${p.ssoName}`}
              portal={p}
              onPress={onLaunch}
              loading={launching === `${p.lob}:${p.ssoName}`}
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
  content: { padding: theme.spacing(3) },
  greeting: { marginBottom: theme.spacing(1.5) },
  hello: { color: theme.colors.textMuted, fontSize: 15 },
  name: {
    color: theme.colors.text,
    fontSize: 28,
    fontWeight: '800',
    letterSpacing: -0.5,
    marginTop: 2,
  },
  email: { color: theme.colors.textMuted, fontSize: 14, marginTop: 2 },
  metaBar: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: theme.spacing(3) },
  metaPill: {
    paddingVertical: 5,
    paddingHorizontal: 12,
    borderRadius: 999,
    backgroundColor: theme.colors.surfaceAlt,
    borderWidth: 1,
    borderColor: theme.colors.border,
  },
  metaPillText: { color: theme.colors.text, fontSize: 12, fontWeight: '700' },
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
  empty: { color: theme.colors.textMuted, fontSize: 14, marginBottom: theme.spacing(2) },
});
