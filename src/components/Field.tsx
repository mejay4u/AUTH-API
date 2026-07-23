import React from 'react';
import { StyleSheet, Text, TextInput, TextInputProps, View } from 'react-native';

import { theme } from '../theme';

interface Props extends TextInputProps {
  label: string;
}

export function Field({ label, style, ...rest }: Props) {
  return (
    <View style={styles.wrap}>
      <Text style={styles.label}>{label}</Text>
      <TextInput
        placeholderTextColor={theme.colors.textMuted}
        style={[styles.input, style]}
        {...rest}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: { marginBottom: theme.spacing(2) },
  label: {
    color: theme.colors.textMuted,
    fontSize: 13,
    marginBottom: 6,
    fontWeight: '600',
    letterSpacing: 0.3,
  },
  input: {
    backgroundColor: theme.colors.inputBg,
    borderColor: theme.colors.border,
    borderWidth: 1,
    borderRadius: theme.radius.md,
    paddingHorizontal: theme.spacing(1.75),
    height: 50,
    color: theme.colors.text,
    fontSize: 16,
  },
});
