# Setup Azure AD tenant
- Create Azure AD tenant.

- Create 'Frontend' app registration (single tenant)
    - Create secret (B-E8Q~p9edy4qUYqv.DNr-5JgjBJ-u9paRnambN5)
    - API permissions > User.ReadWrite.All (Graph API, application) (Should be a separate AR)
    - API permissions > openid (Graph API, delegated)
    - API permissions > offline_access (Graph API, delegated)
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
            { "extension_{frontend_application_id_without_dash}_actors", "test:me test:me1" }
        }
    };

    var userUpdated = await graphClient.Users["{user_object_id}"].Request().UpdateAsync(userUpdate);
    ``` 
- Azure AD > External Identities > User flows > Add User flow (singinflow)
- Azure AD > External Identities > User flows > signinflow > Applications > Add application 'Frontend'

# Setup sign in flow
