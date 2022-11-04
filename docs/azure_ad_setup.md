# Setup Azure Active Directory
- Create Azure Active Directory.
    - AD domain name is formatted as '<env>ADDataHub', e.g. 'dev002ADDataHub'.
    - Activate P2 trial (or buy actual license).

- NOTE: All created app registrations must have "accessTokenAcceptedVersion": 2, in manifest.
- Create 'Frontend' app registration (single tenant)
    - Add SPA authentication for following URLs:
        - [dev-environments only] https://localhost
        - [dev002] https://wonderful-field-057109603.1.azurestaticapps.net/
    - [Optional, debugging only] Create secret.
    - API permissions > User.Read > Remove
    - API permissions > openid (Graph API, delegated)
    - API permissions > offline_access (Graph API, delegated)
    - Authentication > Implicit grant and hybrid flows > ID tokens
    - Add extension 'actors' through Graph API:
        - Create app registration 'GraphClient'
        - Add application permission 'GraphAPI.User.ReadWriteAll'
        - Add application permission 'GraphAPI.Application.ReadWriteAll'
        - API application permissions 'GraphAPI.DelegatedPermissionGrant.ReadWrite.All'
        - Grand admin consent
        - Create client secret
        ```C#
        var extensionProperty = new ExtensionProperty
        {
            Name = "actors",
            DataType = "String",
            TargetObjects = new List<string>()
            {
                "User"
            }
        };

        await graphClient.Applications["{frontend_application_object_id}"].ExtensionProperties
            .Request()
            .AddAsync(extensionProperty);
        ```
        - Add 'extn.actors' claim in Token Configuration > Add optional claims for ID token.

- Create 'Actor 1' and 'Actor 2' app registration (should be done through Graph API)
    - Add application ID to actor AR
    - Add scope to actor (https://<guid>/actor.default)
    - Add permissions to actor as app roles for users.

- For each actor: Frontend AR > API Permissions > Add a permission > My APIs > Add delegated permission
    - Grant admin consent for DH Titans

- Enterprise Applications > Actor n > User/Groups > Add .. > Select user and roles
- Azure AD > Users > User settings > External users > Enable guest self-service sign up via user flows

- Assign actors through graph API
    ```C#
    var userUpdate = new User
    {
        AdditionalData = new Dictionary<string, object>()
        {
            { "extension_{frontend_application_id_without_dash}_actors", "api://<actor_1_app_id>/<scope> api://<actor_n_app_id>/<scope>" }
        }
    };

    var userUpdated = await graphClient.Users["{user_object_id}"].Request().UpdateAsync(userUpdate);
    ``` 
- Azure AD > External Identities > User flows > Add User flow (singinflow)
- Azure AD > External Identities > User flows > signinflow > Applications > Add application 'Frontend'

# Perform a sign in flow
![loginflow](https://user-images.githubusercontent.com/77341673/195361659-338ebe5b-86e7-4113-ac91-f3a8ec7197e2.png)

1. Contact the OAuth2 /authorize endpoint to authorize the user.

```
https://login.microsoftonline.com/<tenant_id>/oauth2/v2.0/authorize?
client_id=<frontend_app_id>& // Authorize user to access the Frontend AR.
nonce=<nounce>& // Required to prevent token replays, when using id_token.
scope=openid offline_access& // openid scope for id_token, offline_access scope for refresh_token
response_type=id_token+code // Use hybrid flow: id_token for the user, including actors; code for access_token.
```

The response will contain an authorization code and an id_token.
The id_token will contain the following claim:
```
{
    ...,
    "extn.actors": [
        "api://<actor_app_id>/<scope>"
    ]
}
```

2. Contact the OAuth2 /token endpoint to get an access token for an actor.

```
https://login.microsoftonline.com/<tenant_id>/oauth2/v2.0/token
FORM:
client_id=<frontend_app_id> // Authorize user to access the Frontend AR.
client_secret=<frontend_client_secret> // Authorize user to access the Frontend AR.
grant_type=authorization_code
code=<authorization_code_from_auth_endpoint>
scope=api://<actor_app_id>/<scope> // The scope of the actor AR.
```

The response will contain access_token and refresh_token.
The roles claim will contain the app roles granted to the user through the actor.

```
{
    ...,
    "roles": [
      "metering_point:create"
    ],
}
```

The access_token can now be passed to APIs.

# Perform a change actor flow
![changeactorflow](https://user-images.githubusercontent.com/77341673/195361685-2e6c79f7-4738-4f6a-a0d9-a3ca8e2a310c.png)

1. It is assumed that the user has already been authenticated and has a valid refresh token, see [sign-in flow](#perform-a-sign-in-flow).
2. Use the refresh_token with OAuth2 /token endpoint to request access token to another actor.

```
https://login.microsoftonline.com/<tenant_id>/oauth2/v2.0/token
FORM:
client_id=<frontend_app_id> // Authorize user to access the Frontend AR.
client_secret=<frontend_client_secret> // Authorize user to access the Frontend AR.
grant_type=refresh_token
refresh_token=<refresh_token>
scope=api://<actor_app_id>/<scope> // The scope of the actor AR.
```

The response will contain a new access_token for this other actor.
The roles claim will contain the app roles granted to the user through the actor.

```
{
    ...,
    "roles": [
      "metering_point:delete"
    ],
}
```

The access_token can now be passed to APIs.

> NOTE: It is important that the access token only grants access to one actor/scope at a time. If more than one scope is specified to the /token endpoint, the following error is received: `AADSTS28000: Provided value for the input parameter scope is not valid because it contains more than one resource. Scope <x> <y> is not valid.`. This behaviour is a requirement.

# SeaURLs

```
curl --request GET \
  --url 'https://login.microsoftonline.com/<tenant_id>/oauth2/v2.0/authorize?client_id=<client_id>&nonce=defaultNonce&scope=openid%20offline_access&response_type=code'

curl --request POST \
  --url https://login.microsoftonline.com/<tenant_id>/oauth2/v2.0/token \
  --header 'Content-Type: application/x-www-form-urlencoded' \
  --data 'client_secret=<client_secret>' \
  --data 'client_id=<client_id>' \
  --data 'grant_type=<authorization_code>' \
  --data 'scope=<actor>' \
  --data 'code=<auth_code>'

curl --request POST \
  --url https://login.microsoftonline.com/<tenant_id>/oauth2/v2.0/token \
  --header 'Content-Type: application/x-www-form-urlencoded' \
  --data 'client_secret=<client_secret>' \
  --data 'client_id=<client_id>' \
  --data 'grant_type=<refresh_token>' \
  --data 'scope=<actor_scope>' \
  --data 'refresh_token=<refresh_token>'
```
