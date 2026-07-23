import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import React, { useLayoutEffect, useState } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { WebView, type WebViewNavigation } from 'react-native-webview';

import { config } from '../config';
import type { RootStackParamList } from '../navigation/types';
import { theme } from '../theme';

type Props = NativeStackScreenProps<RootStackParamList, 'SsoWebView'>;

/**
 * Opens the complete federated sign-on URL returned by GET /api/v1/sso. The URL already
 * carries the OpenToken hand-off, so the portal (e.g. HRA) logs the member in on load —
 * this screen just hosts the resulting web session.
 */
export function SsoWebViewScreen({ route, navigation }: Props) {
  const { portalName, url } = route.params;
  const [loading, setLoading] = useState(true);

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

  function onNavChange(navState: WebViewNavigation) {
    const current = navState.url ?? '';
    const matched = config.ssoSuccessUrlPrefixes.some((p) => p && current.startsWith(p));
    if (matched) navigation.goBack();
  }

  return (
    <SafeAreaView style={styles.safe} edges={['bottom']}>
      <WebView
        source={{ uri: url }}
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
