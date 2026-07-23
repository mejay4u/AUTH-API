import { DefaultTheme, NavigationContainer } from '@react-navigation/native';
import { StatusBar } from 'expo-status-bar';
import React from 'react';
import { SafeAreaProvider } from 'react-native-safe-area-context';

import { AuthProvider } from './src/auth/AuthContext';
import { RootNavigator } from './src/navigation/RootNavigator';
import { theme } from './src/theme';

const navTheme = {
  ...DefaultTheme,
  colors: {
    ...DefaultTheme.colors,
    background: theme.colors.background,
    card: theme.colors.background,
    text: theme.colors.text,
    border: theme.colors.border,
    primary: theme.colors.primary,
  },
};

export default function App() {
  return (
    <SafeAreaProvider>
      <AuthProvider>
        <NavigationContainer theme={navTheme}>
          <StatusBar style="dark" />
          <RootNavigator />
        </NavigationContainer>
      </AuthProvider>
    </SafeAreaProvider>
  );
}
