# Energinet.DataHub.MarketParticipant.Client Release notes

## Version 2.36.5

- Added new Marketrole, Delegated

## Version 2.36.4

- No functional change.

## Version 2.36.3

- No functional change.

## Version 2.36.2

- No functional change.

## Version 2.36.1

- Added new Marketrole, Meter operator

## Version 2.36.0

- Removed GridAreaOverview as it now comes from swagger

## Version 2.35.0

- Removed Actor Auditlog as it now comes from swagger

## Version 2.34.0

- Added Actor Auditlog

## Version 2.33.0

- Remove assigned actors from user overview.

## Version 2.32.6

- API for resetting users 2FA.

## Version 2.32.5

- Added domain to OrganizationChangeDto

## Version 2.32.4

- Organization audit log API.

## Version 2.32.3

- Remove ElOverblik from market roles.

## Version 2.32.2

- Updated audit log model for user roles.

## Version 2.32.1

- Added /audit-identity controller.

## Version 2.32.0

- Changed ChangedByUserId to AuditIdentityId for audited types.

## Version 2.31.8

- Bumped .github to use v12

## Version 2.31.7

- Add user identity audit logging for status changes.

## Version 2.31.6

- Add user identity audit logs.

## Version 2.31.5

- Made route to resend user invitation RESTful.

## Version 2.31.4

- Deactivate user API

## Version 2.31.3

- Updated UserStatus Enum

## Version 2.31.2

- Adding endpoint for resend user invitation

## Version 2.31.1

- No functional change.

## Version 2.31.0

First name and last name replacing name in user overview.

## Version 2.30.3

No function change

## Version 2.30.2

- Added endpoint for initiating MitID signup.

## Version 2.30.1

- Added endpoint for updating a user phone number

## Version 2.29.6

- Added endpoint to get all actors.

## Version 2.29.5

- No functional change.

## Version 2.29.4

- Add endpoint for getting a single detailed permission

## Version 2.29.3

- Add missing enum value for UserRoleAssignmentTypeAuditLog

## Version 2.29.2

- Add new endpoint to deactivate a user role

## Version 2.29.1

- Bump version as part of pipeline change.

## Version 2.29.0

- Removed endpoint to get which market roles are assigned to a given permission
- Assignable market roles are now part of permissions

## Version 2.28.2

- Added endpoint to get which market roles are assigned to a given permission

## Version 2.28.1

- Added endpoint to get which user roles are assigned to a given permission

## Version 2.28.0

- dotnet 7.

## Version 2.27.0

- Updating permission details response with Created date

## Version 2.26.0

- Adding api for permission change audit logs

## Version 2.24.0

- Adding api for updating permission details

## Version 2.23.0

- Actors are no longer placed under an organization.

## Version 2.22.0

- Get all permissions

## Version 2.21.0

- Updated user audit log with invites
- Added user id to user role assignment audit log

## Version 2.20.0

- UserRoleWithPermissionsDto updated to contain list of permission details
- Renamed SelectablePermission to PermissionDetails

## Version 2.19.0

- Required domain name when creating organization.

## Version 2.18.0

- Add API for inviting users.
- Package update.

## Version 2.17.4

- Added EicFunction parameter to GetSelectablePermission Endpoint

## Version 2.17.3

- Removed unused market roles.

## Version 2.17.2

- Bump version as part of pipeline change.

## Version 2.17.1

- User role with permissions changed to return numbers.

## Version 2.17.0

- Removed unused API for getting users.

## Version 2.16.0

- Sort properties for user overview.

## Version 2.15.11

- Fix missing query parameters to user overview.

## Version 2.15.10

- Added filter by user roles to users overview.

## Version 2.15.9

- New market roles.

## Version 2.15.8

- Created separate actor model to avoid conflict.

## Version 2.15.7

- Extend user overview with user role information.

## Version 2.15.6

- Changed type of variable for permissions to be Ints

## Version 2.15.5

- Added endpoint for updating a user role

## Version 2.15.4

- Bump version as part of pipeline change.

## Version 2.15.3

- Added endpoint for return a list of available permissions in the system

## Version 2.15.2

- Added endpoint for returning user role audit logs

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

- For actors: GetActorsAsync, GetActorAsync, CreateActorAsync, UpdateActorAsync
- For Organizations: GetOrganizationsAsync, GetOrganizationAsync, CreateOrganizationAsync, UpdateOrganizationAsync

## Version 0.2.1

- Preparing package for release.
