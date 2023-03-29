# Guide for configuring B2C after IaC run

There are currently parts of infrastructure that cannot be configured through TF or Graph API. These require manual configuration for each environment. The steps can be performed after 'shared-resources' and 'ui' have been deployed.

## Configuring 'frontend-app' application registration

Change the `"allowPublicClient": false` property in the application manifest to `"allowPublicClient": null`, then Save. Note that sometimes it can take a lot of time (30 min+) for the change to apply.

## Configuring 'B2C_1_InvitationFlow' user flow

Under Properties pane, apply the following settings.

- Multifactor authentication - Type of method - SMS only
- Multifactor authentication - MFA enforcement - Always on

Under Page layouts pane, apply the following settings.

> NOTE: Replace `<host-name>` with environment-specific host name, e.g. jolly-sand-03f839703.azurestaticapps.net.

- Layout name 'Forgot password page' - Use custom page content - Yes
- Layout name 'Forgot password page' - Custom page URI - `https://<host-name>/assets/b2c-custom-layouts/forgotpassword-unified.html`
- Layout name 'Change password page' - Use custom page content - Yes
- Layout name 'Change password page' - Custom page URI - `https://<host-name>/assets/b2c-custom-layouts/changepassword-unified.html`
- Layout name 'Multifactor authentication page' - Use custom page content - Yes
- Layout name 'Multifactor authentication page' - Custom page URI - `https://<host-name>/assets/b2c-custom-layouts/multifactorauth-unified.html`

## Configuring 'B2C_1_SignInFlow' user flow

Under Properties pane, apply the following settings.

- Multifactor authentication - Type of method - Authenticator app - TOTP
- Multifactor authentication - MFA enforcement - Always on
- Token lifetime - Access & ID token lifetime (minutes) - 30
- Token lifetime - Refresh token lifetime (days) - 1
- Token lifetime - Refresh token sliding window lifetime - Bounded
- Token lifetime - Lifetime length (days) - 1
- Session behavior - Web app session lifetime (minutes) - 15
- Session behavior - Web app session timeout - Absolute

Under Page layouts pane, apply the following settings.

> NOTE: Replace `<host-name>` with environment-specific host name, e.g. jolly-sand-03f839703.azurestaticapps.net.

- Layout name 'Sign in page' - Use custom page content - Yes
- Layout name 'Sign in page' - Custom page URI - `https://<host-name>/assets/b2c-custom-layouts/login-unified.html`
