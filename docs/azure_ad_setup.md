# Setup Azure AD tenant
- Create Azure AD tenant.

- Create 'Frontend' app registration (single tenant)
    - Create secret (B-E8Q~p9edy4qUYqv.DNr-5JgjBJ-u9paRnambN5)
    - API permissions > User.ReadWrite.All (Graph API, application) (Should be a separate AR)
    - API permissions > openid (Graph API, delegated)
    - API permissions > offline_access (Graph API, delegated)
    - Authentication > Implicit grant and hybrid flows > ID tokens.
    - Add extension 'actors' through Graph API:
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

- Create 'Actor 1' and 'Actor 2' app registration (should be done through Graph API)
    - Add permission 'metering_point:create' to actor as app roles for users.
    - Add application ID to actor AR
    - Add scope to actor (actor_id, admins only, Actor n name, Name of actor n, enabled)

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

1. Refresh token
2. Profit

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
