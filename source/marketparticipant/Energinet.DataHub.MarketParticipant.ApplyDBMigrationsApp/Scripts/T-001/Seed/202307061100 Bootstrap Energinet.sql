-- Create Grid Areas
INSERT INTO [dbo].[GridArea] (
    [Code],
    [Name],
    [PriceAreaCode],
    [ValidFrom],
    [ValidTo],
    [FullFlexDate]
)
VALUES
    ('533', 'Netområde 533', 1, '2000-01-01', NULL, NULL),
    ('543', 'Netområde 543', 1, '2000-01-01', NULL, NULL),
    ('584', 'Netområde 584', 1, '2000-01-01', NULL, NULL),
    ('803', 'Netområde 803', 1, '2000-01-01', NULL, NULL),
    ('804', 'Netområde 804', 1, '2000-01-01', NULL, NULL);

-- Create Energinet Organisation
DECLARE @organisationId varchar(36) = '10000000-0000-0000-0000-000000000000'

INSERT INTO [dbo].[Organization] (
    [Id],
    [Name],
    [BusinessRegisterIdentifier],
    [Address_StreetName],
    [Address_Number],
    [Address_ZipCode],
    [Address_City],
    [Address_Country],
    [Comment],
    [Status],
    [Domain]
)
VALUES
    (
    @organisationId,
	'Energinet DataHub A/S',
	'39315041',
	'Tonne Kjærsvej',
	'65',
	'7000',
	'Fredericia',
	'DK',
	NULL,
	2,
	'energinet.dk');

-- Create Energinet Actor
DECLARE @actorId varchar(36) = '00000000-0000-0000-0000-000000000001'

INSERT INTO [dbo].[Actor] (
    [Id],
    [OrganizationId],
    [ActorId],
    [ActorNumber],
    [Status],
    [Name],
    [IsFas]
)
VALUES
    (
        @actorId,
        @organisationId,
        '00000000-0000-0000-0000-000000000001', -- INSERT B2C APP REGISTRATION GUID HERE
        '5790001330583',
        2,
        'Energinet DataHub A/S (DataHub systemadministrator)',
        1);

-- Assign Market Role
INSERT INTO [dbo].[MarketRole] (
	[Id],
    [ActorId],
    [Function],
    [Comment]
)
VALUES
    (NEWID(), @actorId, 50, NULL);

-- Create Bootstrap User
DECLARE @userId varchar(36) = '00000000-0000-1111-0000-000000000000'

INSERT INTO [dbo].[User] (
    [Id],
    [ExternalId],
    [Email],
    [MitIdSignupInitiatedAt],
    [SharedReferenceId],
    [InvitationExpiresAt],
    [AdministratedByActorId]
)
VALUES
    (
        @userId,
        '00000000-0000-1111-0000-000000000000', -- INSERT B2C USER GUID HERE
        'bootstrap.datahub@energinet.dk',
        NULL,
        '00000000-0000-1111-0000-000000000000',
        NULL,
        @actorId);

-- Assign User Role to User
DECLARE @userAdminRoleId varchar(36) = 'f3df856f-bd11-4174-97cb-fb6bc54c300a'

INSERT INTO [dbo].[UserRoleAssignment] (
    [UserId],
    [ActorId],
    [UserRoleId]
)
VALUES
    (@userId, @actorId, @userAdminRoleId);

INSERT INTO [dbo].[UserRolePermission] (
    [UserRoleId],
    [PermissionId]
)
VALUES
    (@userAdminRoleId, 5),
    (@userAdminRoleId, 6),
    (@userAdminRoleId, 7),
    (@userAdminRoleId, 8);
