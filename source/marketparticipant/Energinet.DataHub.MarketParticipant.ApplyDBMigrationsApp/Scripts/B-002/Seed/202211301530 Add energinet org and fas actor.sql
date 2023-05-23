DECLARE @organizationId varchar(36) = NEWID()

INSERT INTO dbo.Organization(Id, [Name], BusinessRegisterIdentifier, [Status], Address_Country)
VALUES(@organizationId,'Energinet DataHub A/S','39315041', 2, 'DK')

INSERT INTO dbo.Actor(Id, ActorNumber, [Status], [Name], OrganizationId, IsFas)
VALUES(NEWID(), '5790001330583', 2, 'Energinet DataHub A/S (DataHub systemadministrator)', @organizationId, 1)
