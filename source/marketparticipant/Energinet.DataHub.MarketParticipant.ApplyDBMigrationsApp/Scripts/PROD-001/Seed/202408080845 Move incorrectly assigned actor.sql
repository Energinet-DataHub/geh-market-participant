DECLARE @organizationId varchar(36) = NEWID();

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
    [Domain],
    [ChangedByIdentityId]
)
VALUES
    (
    @organizationId,
	'True Energy A/S',
	'39406764',
	NULL,
	NULL,
	NULL,
	NULL,
	'DK',
	NULL,
	2,
	'trueenergy.dk',
    '00000000-FFFF-FFFF-FFFF-000000000000');

UPDATE [dbo].[Actor]
SET [OrganizationId] = @organizationId
WHERE [Id] = 'f8ee6352-9335-4f4f-7f5c-08dc5d4613fc' and [ActorNumber] = '5790002470271'
