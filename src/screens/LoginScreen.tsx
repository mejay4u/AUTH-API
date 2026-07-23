import React, { useState } from 'react';
import {
  KeyboardAvoidingView,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { ApiError } from '../api/http';
import { useAuth } from '../auth/AuthContext';
import { Button } from '../components/Button';
import { Field } from '../components/Field';
import { theme } from '../theme';

/** Bump this on every change so the running bundle is verifiable on-screen. */
const BUILD = 'sso-dbg-2';

export function LoginScreen() {
  const { signIn, baseUrl, setBaseUrl } = useAuth();

  const [userId, setUserId] = useState('');
  const [password, setPassword] = useState('');
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const canSubmit = userId.trim().length > 0 && password.length > 0 && !submitting;

  async function onSubmit() {
    setError(null);
    setSubmitting(true);
    try {
      // Runs POST /auth/login then POST /auth/completelogin under the hood.
      await signIn(userId.trim(), password);
      // On success the navigator swaps to the Home stack automatically.
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <SafeAreaView style={styles.safe}>
      <KeyboardAvoidingView
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
        style={styles.flex}
      >
        <ScrollView
          contentContainerStyle={styles.content}
          keyboardShouldPersistTaps="handled"
        >
          <View style={styles.header}>
            <Text style={styles.brand}>Member Portal</Text>
            <Text style={styles.subtitle}>
              Sign in to access your benefits and single sign-on portals.
            </Text>
            <Text style={styles.build}>build: {BUILD}</Text>
          </View>

          <Field
            label="User ID"
            value={userId}
            onChangeText={setUserId}
            autoCapitalize="none"
            autoCorrect={false}
            keyboardType="email-address"
            textContentType="username"
            placeholder="you@example.com"
            returnKeyType="next"
          />

          <Field
            label="Password"
            value={password}
            onChangeText={setPassword}
            secureTextEntry
            autoCapitalize="none"
            textContentType="password"
            placeholder="••••••••"
            returnKeyType="go"
            onSubmitEditing={() => canSubmit && onSubmit()}
          />

          {error ? <Text style={styles.error}>{error}</Text> : null}

          <Button
            title="Sign in"
            onPress={onSubmit}
            loading={submitting}
            disabled={!canSubmit}
            style={{ marginTop: theme.spacing(1) }}
          />

          <Pressable onPress={() => setShowAdvanced((s) => !s)} style={styles.advancedToggle}>
            <Text style={styles.advancedToggleText}>
              {showAdvanced ? 'Hide' : 'Advanced'} · server settings
            </Text>
          </Pressable>

          {showAdvanced ? (
            <View style={styles.advanced}>
              <Field
                label="API base URL"
                value={baseUrl}
                onChangeText={setBaseUrl}
                autoCapitalize="none"
                autoCorrect={false}
                keyboardType="url"
                placeholder="http://192.168.x.x:38340"
              />
              <Text style={styles.hint}>
                On a physical device use your computer's LAN IP, not localhost.
              </Text>
            </View>
          ) : null}
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: theme.colors.background },
  flex: { flex: 1 },
  content: {
    padding: theme.spacing(3),
    flexGrow: 1,
    justifyContent: 'center',
  },
  header: { marginBottom: theme.spacing(4) },
  brand: {
    color: theme.colors.text,
    fontSize: 32,
    fontWeight: '800',
    letterSpacing: -0.5,
  },
  subtitle: {
    color: theme.colors.textMuted,
    fontSize: 15,
    marginTop: 6,
    lineHeight: 21,
  },
  build: {
    color: theme.colors.success,
    fontSize: 12,
    marginTop: 8,
    fontWeight: '700',
    letterSpacing: 0.5,
  },
  error: {
    color: theme.colors.danger,
    marginBottom: theme.spacing(1),
    fontSize: 14,
  },
  advancedToggle: { alignSelf: 'center', marginTop: theme.spacing(3), padding: 8 },
  advancedToggleText: { color: theme.colors.textMuted, fontSize: 13, fontWeight: '600' },
  advanced: { marginTop: theme.spacing(1) },
  hint: { color: theme.colors.textMuted, fontSize: 12, marginTop: -6 },
});
