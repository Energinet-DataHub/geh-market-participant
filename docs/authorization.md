# Authorization

The authorization is based on OAuth claims granted by an authorization server. The granted claims are placed in a JWT access token within the "roles"-claim. Each claim value represents and grants access to a single permission in DataHub. The `geh-market-participant` repository is responsible for maintaining and issuing permissions to users.

As an example, the payload of an access token giving permissions `organizations:view` and `grid-areas:manage` will look as follows.

```Json
{
  "sub": "<user-id>",
  "azp": "<actor-id>",
  "token": "<access-token-from-AD>",
  "membership": "fas",
  "roles": ["organizations:view", "grid-areas:manage"]
}
```

Domains can either validate the token using the middleware provided in [Energinet.DataHub.Core.App.WebApp](https://github.com/Energinet-DataHub/geh-core/blob/main/source/App/documents/authorization.md) or use another OIDC approach. The OIDC configuration endpoint is `https://app-webapi-markpart-<environment>.azurewebsites.net/.well-known/openid-configuration`. APIM and BFF are also configured to validate the tokens.

## Signing Key

The tokens are signed using an asymmetric RSA key `token-sign` maintained in the Azure Key Vault `kv-main-sharedres-<environment>`. The public part of the key is returned for use with OpenId configuration through the `/token/keys` endpoint in `geh-market-participant`. The private part of the key is known only to Azure; the tokens are hashed and the hash is signed by the key vault.

Currently, the key does not expire and is not rotated. This must be reconfigured before production.

## Adding New Permissions

Permissions managed by DataHub must be registered in the `KnownPermissions` class, located in `Energinet.DataHub.MarketParticipant.Infrastructure.Model.Permissions`-namespace.

Adding or editing a permission can be done in three steps:

1) Add a new permission id to `PermissionId` enum.
2) Register the new permission in `KnownPermissions` class.
3) Create a PR in `geh-market-participant` and wait for it to be completed.
4) (Optional) If the permission is needed to guard features in `greenforce-frontend`, add the claim entry to `libs\dh\shared\feature-authorization\src\lib\permission.ts`.

When creating the `KnownPermissions` class, specify the following:

- The newly created `PermissionId`.
- The claim to be used for the given permission. The claim will be sent with the token and shown to the users in the UI. Use a simple and concise value; the convention is *[kebab-cased-plural-feature-name]:[access]*, e.g. `grid-areas:manage` or `organizations:view`.
- The date for when the permission has been introduced. Used for audit logs only.
- A list of market roles that may support the permission. If a given actor or a given user role does not belong to any of the market roles on this list, the permission will not apply to them.

### The permissions are now available for use

> Please be aware of the following caveats!

- Remember to assign the newly added permission to a user role through the UI.
- It is safe to rename the permissions in the `PermissionId` enum.
- It is safe to rename the claim entries, but be aware that user will lose access to the permission until their token expires.
- Be CAUTIOUS when changing the numeric values of the `PermissionId` enum.
- It is NOT SAFE to reuse the numeric values of the `PermissionId` enum.

## Login Sequence

![User sign-in sequence diagram (Short)](https://user-images.githubusercontent.com/77341673/206713883-70f26640-0f45-46fd-9871-f829e73f465a.png)

## Overview of Components

![geh-market-participant-resources](https://user-images.githubusercontent.com/77341673/206713903-e529e95e-965e-4024-96ae-4243d0c3eccf.png)
