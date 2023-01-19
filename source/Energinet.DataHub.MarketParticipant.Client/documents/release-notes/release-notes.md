# Energinet.DataHub.MarketParticipant.Client Release notes

## Version 2.15.2

- Added api endpoint for returning user role audit logs

## Version 2.15.1

- Update user role endpoint with response including permissions.

## Version 2.15.0

- Update user overview API with user status.

## Version 2.14.9

- Extend user overview with active filter.

## Version 2.14.8

- Add create user role API
- Change response types in Get all user roles to return user role status enum instead of integer

## Version 2.14.7

- Updated packages.

## Version 2.14.6

- Actor name in actor selection API.

## Version 2.14.5

- Get all user roles return type update.

## Version 2.14.4

- Add get all user roles API.

## Version 2.14.3

- Add get user role API.

## Version 2.14.2

- Add get user API.

## Version 2.14.1

- Add user audit log API.

## Version 2.14.0

- Split update roles API into two added and removed collections.

## Version 2.13.0

- Add total number of users to help with paging.

## Version 2.12.1

- Added optional searchText param to user overview

## Version 2.12.0

- Simplify call to assignment of user roles.

## Version 2.11.0

- Organization name for each actor in actor selection API.

## Version 2.10.0

- Rename user role to not contain 'template'.

## Version 2.9.6

- Added endpoint to get actors to a user.

## Version 2.9.4

- Added endpoints for actor selection.

## Version 2.9.3

- Updated endpoint for getting user overview.

## Version 2.9.2

- Updated internal registrations.

## Version 2.9.1

- Add endpoint for getting user role templates.

## Version 2.9.0

- Add endpoint for getting user overview.

## Version 2.7.7

- Fixed query parameter.

## Version 2.7.6

- Add endpoint for getting actors for user.

## Version 2.7.5

- Add token endpoint.

## Version 2.7.3

- Updated packages.

## Version 2.7.2

- Updated deployment, no code changes.

## Version 2.7.1

- Show correct exception in backend for frontend.

## Version 2.7.0

- Removed actor status deleted

## Version 2.6.0

- User display name on audit log entry

## Version 2.5.2

- API for retrieving audit log entries for a grid area.

## Version 2.5.1

- Do not handle unexpected exceptions.

## Version 2.5.0

- Added Grid name update

## Version 2.4.0

- Price area code is now an enumeration.

## Version 2.3.0

- Added FullFlexDate

## Version 2.2.5

- Updated packages

## Version 2.2.1

- Pipeline updated

## Version 2.0.3 -> 2.2.0

- Unknown

## Version 2.0.3

- Added grid areas to actor.

## Version 2.0.1

- Added missing metering point types.

## Version 2.0.0

- .NET 6 upgrade

## Version 0.5.4

- Fixed incorrect null properties.

## Version 0.5.3

- Added error handling.

## Version 0.5.2

- Added Comment to Organization

## Version 0.5.1

- Added contacts

## Version 0.4.1

- Added CVR and Address to organization

## Version 0.4.0

- Global location number cannot be changed on an actor.

## Version 0.3.1

### Added methods for the following endpoints

- For actors
    - GetActorsAsync
    - GetActorAsync
    - CreateActorAsync
    - UpdateActorAsync
- For Organizations
    - GetOrganizationsAsync
    - GetOrganizationAsync
    - CreateOrganizationAsync
    - UpdateOrganizationAsync

## Version 0.2.1

- Preparing package for release.
