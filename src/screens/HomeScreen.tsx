import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import * as WebBrowser from 'expo-web-browser';
import React, { useCallback, useState } from 'react';
import { Alert, Linking, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
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

/**
 * Where an SSO URL opens:
 *  - 'webview'  : the app's embedded WebView (react-native-webview), fully in-app, isolated cookies.
 *  - 'inapp'    : an in-app system browser (SFSafariViewController / Chrome Custom Tab) via
 *                 expo-web-browser — stays in-app but shares Safari cookies; best for SSO.
 *  - 'external' : the device's default browser app (Safari/Chrome) via Linking.
 */
type BrowserMode = 'webview' | 'inapp' | 'external';

const MODE_LABEL: Record<BrowserMode, string> = {
  webview: 'Embedded',
  inapp: 'In-app',
  external: 'Browser',
};

const MODE_HINT: Record<BrowserMode, string> = {
  webview: 'Embedded WebView — stays in the app, isolated session.',
  inapp: 'In-app Safari — stays in the app, shares your Safari session (best for SSO).',
  external: 'Default browser — opens Safari/Chrome outside the app.',
};

export function HomeScreen({ navigation }: Props) {
  const { session, signOut } = useAuth();
  const [launching, setLaunching] = useState<string | null>(null);
  const [mode, setMode] = useState<BrowserMode>('inapp');

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
        if (mode === 'webview') {
          navigation.navigate('SsoWebView', { portalName: portal.name, url: res.ssoUrl });
          return;
        }
        if (mode === 'inapp') {
          // In-app system browser (SFSafariViewController / Chrome Custom Tab).
          await WebBrowser.openBrowserAsync(res.ssoUrl);
          return;
        }
        // External default browser (Safari/Chrome).
        const canOpen = await Linking.canOpenURL(res.ssoUrl);
        if (!canOpen) {
          Alert.alert(portal.name, 'No browser is available to open this link.');
          return;
        }
        await Linking.openURL(res.ssoUrl);
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
    [session, navigation, signOut, mode],
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

        <View style={styles.segment}>
          {(['webview', 'inapp', 'external'] as BrowserMode[]).map((m) => (
            <Pressable
              key={m}
              onPress={() => setMode(m)}
              style={[styles.segmentBtn, mode === m && styles.segmentBtnActive]}
            >
              <Text style={[styles.segmentText, mode === m && styles.segmentTextActive]}>
                {MODE_LABEL[m]}
              </Text>
            </Pressable>
          ))}
        </View>
        <Text style={styles.segmentHint}>{MODE_HINT[mode]}</Text>

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
  segment: {
    flexDirection: 'row',
    backgroundColor: theme.colors.surface,
    borderWidth: 1,
    borderColor: theme.colors.border,
    borderRadius: theme.radius.md,
    padding: 4,
    marginBottom: 8,
  },
  segmentBtn: {
    flex: 1,
    paddingVertical: 9,
    borderRadius: theme.radius.sm,
    alignItems: 'center',
  },
  segmentBtnActive: { backgroundColor: theme.colors.primary },
  segmentText: { color: theme.colors.textMuted, fontSize: 13, fontWeight: '700' },
  segmentTextActive: { color: theme.colors.primaryText },
  segmentHint: {
    color: theme.colors.textMuted,
    fontSize: 12,
    marginBottom: theme.spacing(2),
    lineHeight: 17,
  },
  empty: { color: theme.colors.textMuted, fontSize: 14, marginBottom: theme.spacing(2) },
});
