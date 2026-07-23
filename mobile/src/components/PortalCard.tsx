import React from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';

import type { SsoPortal } from '../api/types';
import { theme } from '../theme';

interface Props {
  portal: SsoPortal;
  onPress: (portal: SsoPortal) => void;
  loading?: boolean;
}

export function PortalCard({ portal, onPress, loading = false }: Props) {
  const accent = portal.accent ?? theme.colors.primary;
  return (
    <Pressable
      onPress={() => onPress(portal)}
      disabled={loading}
      style={({ pressed }) => [styles.card, pressed && styles.pressed]}
    >
      <View style={[styles.badge, { backgroundColor: accent }]}>
        <Text style={styles.badgeText}>{portal.ssoName.slice(0, 3).toUpperCase()}</Text>
      </View>
      <View style={styles.body}>
        <Text style={styles.name}>{portal.name}</Text>
        {portal.description ? (
          <Text style={styles.desc} numberOfLines={2}>
            {portal.description}
          </Text>
        ) : null}
        <Text style={[styles.cta, { color: accent }]}>
          {loading ? 'Connecting…' : 'Log in via SSO  →'}
        </Text>
      </View>
      {loading ? <ActivityIndicator color={accent} style={styles.spinner} /> : null}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  card: {
    flexDirection: 'row',
    backgroundColor: theme.colors.surface,
    borderRadius: theme.radius.lg,
    borderWidth: 1,
    borderColor: theme.colors.border,
    padding: theme.spacing(2),
    marginBottom: theme.spacing(1.5),
    alignItems: 'center',
  },
  pressed: { opacity: 0.75 },
  badge: {
    width: 52,
    height: 52,
    borderRadius: theme.radius.md,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: theme.spacing(2),
  },
  badgeText: { color: '#06210f', fontWeight: '800', fontSize: 14 },
  body: { flex: 1 },
  name: { color: theme.colors.text, fontSize: 17, fontWeight: '700' },
  desc: { color: theme.colors.textMuted, fontSize: 13, marginTop: 2 },
  cta: { marginTop: 8, fontSize: 14, fontWeight: '700' },
  spinner: { marginLeft: theme.spacing(1) },
});
