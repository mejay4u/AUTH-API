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
import { config } from '../config';
import { theme } from '../theme';

export function LoginScreen() {
  const { signIn, baseUrl, setBaseUrl } = useAuth();

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [lob, setLob] = useState<string>(config.lobs[0]);
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const canSubmit = username.trim().length > 0 && password.length > 0 && !submitting;

  async function onSubmit() {
    setError(null);
    setSubmitting(true);
    try {
      await signIn(username.trim(), password, lob);
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
          </View>

          <Field
            label="Username"
            value={username}
            onChangeText={setUsername}
            autoCapitalize="none"
            autoCorrect={false}
            textContentType="username"
            placeholder="e.g. jdoe"
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

          <Text style={styles.label}>Line of business</Text>
          <View style={styles.lobRow}>
            {config.lobs.map((code) => {
              const active = code === lob;
              return (
                <Pressable
                  key={code}
                  onPress={() => setLob(code)}
                  style={[styles.chip, active && styles.chipActive]}
                >
                  <Text style={[styles.chipText, active && styles.chipTextActive]}>
                    {code}
                  </Text>
                </Pressable>
              );
            })}
          </View>

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
                placeholder="http://192.168.x.x:5155"
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
  label: {
    color: theme.colors.textMuted,
    fontSize: 13,
    marginBottom: 6,
    fontWeight: '600',
    letterSpacing: 0.3,
  },
  lobRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: theme.spacing(2) },
  chip: {
    paddingVertical: 8,
    paddingHorizontal: 14,
    borderRadius: 999,
    borderWidth: 1,
    borderColor: theme.colors.border,
    backgroundColor: theme.colors.surface,
  },
  chipActive: {
    borderColor: theme.colors.primary,
    backgroundColor: theme.colors.surfaceAlt,
  },
  chipText: { color: theme.colors.textMuted, fontWeight: '600', fontSize: 13 },
  chipTextActive: { color: theme.colors.text },
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
