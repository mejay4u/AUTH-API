export type RootStackParamList = {
  Login: undefined;
  Home: undefined;
  SsoWebView: {
    portalName: string;
    /** The complete federated sign-on URL returned by GET /api/v1/sso. */
    url: string;
  };
};
