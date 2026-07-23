import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import React, { useLayoutEffect, useMemo, useRef, useState } from 'react';
import {
  ActivityIndicator,
  Pressable,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { WebView, type WebViewNavigation } from 'react-native-webview';

import { completeLogon } from '../api/sso';
import { useAuth } from '../auth/AuthContext';
import { config } from '../config';
import type { RootStackParamList } from '../navigation/types';
import { theme } from '../theme';

type Props = NativeStackScreenProps<RootStackParamList, 'SsoWebView'>;

/** Build an auto-submitting HTML form for SAML/OIDC POST binding. */
function buildPostHtml(url: string, fields: Record<string, string>): string {
  const inputs = Object.entries(fields)
    .map(
      ([name, value]) =>
        `<input type="hidden" name="${escapeHtml(name)}" value="${escapeHtml(value)}" />`,
    )
    .join('');
  return `<!DOCTYPE html><html><head><meta name="viewport" content="width=device-width, initial-scale=1" /></head>
<body style="background:${theme.colors.background};margin:0;">
<form id="sso" method="post" action="${escapeHtml(url)}">${inputs}</form>
<script>document.getElementById('sso').submit();</script>
</body></html>`;
}

function escapeHtml(s: string): string {
  return s
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

export function SsoWebViewScreen({ route, navigation }: Props) {
  const { portalCode, portalName, launch } = route.params;
  const { session, getValidAccessToken } = useAuth();

  const [loading, setLoading] = useState(true);
  const completedRef = useRef(false);

  useLayoutEffect(() => {
    navigation.setOptions({
      title: portalName,
      headerRight: () => (
        <Pressable onPress={() => navigation.goBack()} hitSlop={12}>
          <Text style={styles.done}>Done</Text>
        </Pressable>
      ),
    });
  }, [navigation, portalName]);

  const source = useMemo(() => {
    if (launch.method === 'POST' && launch.formFields) {
      return { html: buildPostHtml(launch.url, launch.formFields), baseUrl: launch.url };
    }
    return { uri: launch.url };
  }, [launch]);

  async function finishLogon() {
    if (completedRef.current || !session) return;
    completedRef.current = true;
    try {
      const token = await getValidAccessToken();
      await completeLogon(session.baseUrl, token, portalCode);
    } catch {
      // Best-effort: the portal session is already established in the WebView.
    }
  }

  function onNavChange(navState: WebViewNavigation) {
    const url = navState.url ?? '';
    const matched = config.ssoSuccessUrlPrefixes.some((p) => p && url.startsWith(p));
    if (matched) {
      finishLogon().finally(() => navigation.goBack());
    }
  }

  return (
    <SafeAreaView style={styles.safe} edges={['bottom']}>
      <WebView
        source={source}
        onNavigationStateChange={onNavChange}
        onLoadStart={() => setLoading(true)}
        onLoadEnd={() => setLoading(false)}
        startInLoadingState
        sharedCookiesEnabled
        thirdPartyCookiesEnabled
        domStorageEnabled
        javaScriptEnabled
        style={styles.web}
      />
      {loading ? (
        <View style={styles.overlay} pointerEvents="none">
          <ActivityIndicator color={theme.colors.primary} size="large" />
          <Text style={styles.overlayText}>Connecting to {portalName}…</Text>
        </View>
      ) : null}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: theme.colors.background },
  web: { flex: 1, backgroundColor: theme.colors.background },
  done: { color: theme.colors.primary, fontSize: 16, fontWeight: '700' },
  overlay: {
    ...StyleSheet.absoluteFillObject,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: theme.colors.background,
  },
  overlayText: { color: theme.colors.textMuted, marginTop: theme.spacing(1.5), fontSize: 14 },
});
