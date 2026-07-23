import type { SsoLaunch } from '../api/types';

export type RootStackParamList = {
  Login: undefined;
  Home: undefined;
  SsoWebView: {
    portalCode: string;
    portalName: string;
    launch: SsoLaunch;
  };
};
