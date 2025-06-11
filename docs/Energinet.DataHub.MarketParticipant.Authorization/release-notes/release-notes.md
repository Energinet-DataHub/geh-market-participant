# Energinet.DataHub.MarketParticipant.Authorization release notes

## Version 1.5.1

- Changed how verify is done, it now takes it own object types, since not all information required for an access request, is needed for a verification of the signature

## Version 1.5.1

- Added new request type for the signature to support the measurement yearly sum data request.

## Version 1.5.0

- Extended signature with access periods for MeteringPoint to be used with measurement data request.

## Version 1.4.0

- Moved MeteringPointMasterDataAccessValidation logic to new Authorization.Application project
- implemented verification of MeteringPoint GridArea is owned by requesting Grid Access Provider for the signature creation.

## Version 1.3.0

- Upgraded to .net 9.

## Version 1.2.0

- Rename authorization header name to 'Signature'.

## Version 1.1.1

- Improve IEndpointAuthorizationLogger registration.

## Version 1.1.0

- Added IEndpointAuthorizationContext.
- Added logging support through IEndpointAuthorizationLogger.
- Added RequestSignatureAsync with User Id.

## Version 1.0.2

- Added energysupplier role validation for access validation.

## Version 1.0.1

- Added support for key rotation.

## Version 1.0.0

- First implementation of Authorization

## Version 0.1.1

- Placeholder release.
