CREATE TABLE [dbo].[OrganizationDomain](
    [Id]              [uniqueidentifier] NOT NULL,
    [OrganizationId]  [uniqueidentifier] NOT NULL,
    [Domain]          [nvarchar](255)    NOT NULL,

    CONSTRAINT [PK_OrganizationDomain] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_OrganizationDomain_OrganizationId_Organization FOREIGN KEY ([OrganizationId]) REFERENCES [dbo].[Organization]([Id]),
    CONSTRAINT [UQ_OrganizationDomain_Domain] UNIQUE NONCLUSTERED ([Domain])
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[OrganizationDomain] ADD

    PeriodStart DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN
    CONSTRAINT DF_OrganizationDomain_PeriodStart DEFAULT SYSUTCDATETIME(),
    PeriodEnd DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN
    CONSTRAINT DF_OrganizationDomain_PeriodEnd   DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME(PeriodStart, PeriodEnd),
    
    Version             int              NOT NULL DEFAULT 0,
    ChangedByIdentityId uniqueidentifier NOT NULL
    CONSTRAINT DF_ChangedByIdentityId DEFAULT('00000000-FFFF-FFFF-FFFF-000000000000'),
    DeletedByIdentityId uniqueidentifier NULL
GO

ALTER TABLE [dbo].[OrganizationDomain]
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.[OrganizationDomainHistory]))
GO

ALTER TABLE [dbo].[OrganizationDomain]
DROP CONSTRAINT DF_ChangedByIdentityId
GO

ALTER TABLE [dbo].[OrganizationDomain]
    ADD CONSTRAINT CHK_OrganizationDomain_ChangedByIdentityId_NotEmpty CHECK (ChangedByIdentityId <> '00000000-0000-0000-0000-000000000000'),
        CONSTRAINT CHK_OrganizationDomain_DeletedByIdentityId_NotEmpty CHECK (DeletedByIdentityId <> '00000000-0000-0000-0000-000000000000');
GO

INSERT INTO [dbo].[OrganizationDomain](Id, OrganizationId, Domain, Version, ChangedByIdentityId)
SELECT NEWID(), Id, Domain, Version, ChangedByIdentityId
FROM [dbo].[Organization]
GO

SET TRANSACTION ISOLATION LEVEL SERIALIZABLE; 
BEGIN TRANSACTION;

ALTER TABLE [OrganizationDomain] SET (SYSTEM_VERSIONING = OFF);
GO

INSERT INTO dbo.[OrganizationDomainHistory] (Id, OrganizationId, Domain,PeriodStart, PeriodEnd,Version, ChangedByIdentityId, DeletedByIdentityId)
SELECT NEWID(), Id, Domain, PeriodStart, PeriodEnd, Version, ChangedByIdentityId, null
FROM OrganizationHistory oh1
WHERE EXISTS
	(SELECT * 
	FROM OrganizationHistory oh2
	WHERE oh2.Domain != oh1.Domain
	AND oh2.Id = oh1.Id)
ORDER BY id, PeriodStart;
GO

ALTER TABLE [OrganizationDomain] SET (SYSTEM_VERSIONING = ON (  HISTORY_TABLE = dbo.[OrganizationDomainHistory], DATA_CONSISTENCY_CHECK = ON));
GO

COMMIT TRANSACTION;

ALTER TABLE [dbo].[Organization]
DROP CONSTRAINT [UQ_Organization_Domain]
GO

ALTER TABLE [dbo].[Organization]
DROP COLUMN [Domain];
GO
