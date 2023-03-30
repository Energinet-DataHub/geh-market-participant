# Guide for configuring B2C after IaC run

There are currently parts of infrastructure that cannot be configured through TF or Graph API. These require manual configuration for each environment. The steps can be performed after 'shared-resources' and 'ui' have been deployed.

## Configuring 'frontend-app' application registration

Change the `"allowPublicClient": false` property in the application manifest to `"allowPublicClient": null`, then Save. Note that sometimes it can take a lot of time (30 min+) for the change to apply.

## Configuring 'B2C_1_InvitationFlow' user flow

Under Properties pane, apply the following settings.

- Enable JavaScript enforcing page layout - On
- Multifactor authentication - Type of method - SMS only
- Multifactor authentication - MFA enforcement - Always on

![Invite Flow Properties](https://user-images.githubusercontent.com/77341673/228801159-7320e8bf-97f3-462c-80bb-c2580777aed3.PNG)

Under Page layouts pane, apply the following settings.

> NOTE: Replace `<host-name>` with environment-specific host name, e.g. jolly-sand-03f839703.azurestaticapps.net.

- Layout name 'Forgot password page' - Use custom page content - Yes
- Layout name 'Forgot password page' - Custom page URI - `https://<host-name>/assets/b2c-custom-layouts/forgotpassword-unified.html`
- Layout name 'Change password page' - Use custom page content - Yes
- Layout name 'Change password page' - Custom page URI - `https://<host-name>/assets/b2c-custom-layouts/changepassword-unified.html`
- Layout name 'Multifactor authentication page' - Use custom page content - Yes
- Layout name 'Multifactor authentication page' - Custom page URI - `https://<host-name>/assets/b2c-custom-layouts/multifactorauth-unified.html`

[Invite Flow Page Layout](https://user-images.githubusercontent.com/77341673/228801236-18c95292-8b1b-4e1f-96ce-cc4302413dcc.PNG)

## Configuring 'B2C_1_SignInFlow' user flow!

Under Properties pane, apply the following settings.

- Enable JavaScript enforcing page layout - On
- Multifactor authentication - Type of method - Authenticator app - TOTP
- Multifactor authentication - MFA enforcement - Always on
- Token lifetime - Access & ID token lifetime (minutes) - 30
- Token lifetime - Refresh token lifetime (days) - 1
- Token lifetime - Refresh token sliding window lifetime - Bounded
- Token lifetime - Lifetime length (days) - 1
- Session behavior - Web app session lifetime (minutes) - 15
- Session behavior - Web app session timeout - Absolute

![Sign In Flow Properties 1](https://user-images.githubusercontent.com/77341673/228801340-cb77d4a1-773d-417e-9f22-7dfd7ae6b196.PNG)
![Sign In Flow Properties 2](https://user-images.githubusercontent.com/77341673/228801401-882d0faa-5c1c-4b43-bfbf-f3c4957530e4.PNG)

Under Page layouts pane, apply the following settings.

> NOTE: Replace `<host-name>` with environment-specific host name, e.g. jolly-sand-03f839703.azurestaticapps.net.

- Layout name 'Sign in page' - Use custom page content - Yes
- Layout name 'Sign in page' - Custom page URI - `https://<host-name>/assets/b2c-custom-layouts/login-unified.html`
