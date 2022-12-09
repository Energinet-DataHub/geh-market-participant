# User Authorization

User authorization in DataHub is done through permissions. The `geh-market-participant` repository is responsible for maintaining and issuing permissions to users. The permissions are issued in user's access token (JWT) as claims.

During login, the frontend requests an access token from `geh-market-participant` token-endpoint and includes it with every request to the other domains through the Authorization-header. The domains then validate the token and read the claims to discover the granted permissions.

## Token Endpoints

When APIM, BFF and the individual domains receive the token, they validate it using the OpenId configuration endpoint made available by `geh-market-participant`. The endpoint is environment specific, e.g. `https://app-webapi-markpart-u-001.azurewebsites.net/.well-known/openid-configuration`.

## Signing Key

The tokens are signed using an asymmetric RSA key `token-sign` maintained in the Azure Key Vault `kv-main-sharedres-<environment>`. The public part of the key is returned for use with OpenId configuration through the `/token/keys` endpoint in `geh-market-participant`. The private part of the key is known only to Azure; the tokens are hashed and the hash is signed by the key vault.

Currently, the key does not expire and is not rotated. This must be reconfigured before production.

## Login Sequence

![User sign-in sequence diagram (Short)](https://user-images.githubusercontent.com/77341673/206713883-70f26640-0f45-46fd-9871-f829e73f465a.png)

## Overview of Components

![geh-market-participant-ressources drawio](https://user-images.githubusercontent.com/77341673/206713903-e529e95e-965e-4024-96ae-4243d0c3eccf.png)
