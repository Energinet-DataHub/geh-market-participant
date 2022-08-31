# Energinet.DataHub.MarketParticipant.Integration.Model Release notes

## Version 2.5.1

- Updated packages

## Version 2.4.2

- Pipeline updated

## Version 1.1.0 -> 2.4.1

- Unknown

## Version 1.1.0

- Preparing package for release.

## Version 1.2.0

- Added integration events.

## Version 1.2.1

- Added GridAreaLinkId to GridArea Integration event

## Version 1.3.0

- Adding shared contract parser returning base integration event.
- Use of this new parser is required.

## Version 2.0.0

- .NET 6 upgrade

## Version 2.0.1

- Public parse methods for helping with integration testing.

## Version 2.0.3

- Added metering point types.

## Version 2.0.4

- Address fields can no longer be null, but may be empty.

## Version 2.0.5

- External Actor Id is annotated to be null. This will happen if the status is New or Deleted, or the chosen roles do not give permission to the actor.

## Version 2.5.0

- Merged eventparsers to a single shared parser, this is an internal change so no actions is required from consumers
