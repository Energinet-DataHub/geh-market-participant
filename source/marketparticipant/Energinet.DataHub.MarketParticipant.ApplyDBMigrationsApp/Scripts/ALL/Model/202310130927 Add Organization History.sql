ALTER TABLE [dbo].[Organization] ADD

    PeriodStart DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN
    CONSTRAINT DF_Organization_PeriodStart DEFAULT SYSUTCDATETIME(),
    PeriodEnd DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN
    CONSTRAINT DF_Organization_PeriodEnd   DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME(PeriodStart, PeriodEnd),
    
    Version             int              NOT NULL DEFAULT 0,
    ChangedByIdentityId uniqueidentifier NOT NULL
    CONSTRAINT DF_ChangedByIdentityId DEFAULT('00000000-FFFF-FFFF-FFFF-000000000000');
GO

ALTER TABLE [dbo].[Organization]
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.[OrganizationHistory]));
GO

ALTER TABLE [dbo].[Organization]
DROP CONSTRAINT DF_ChangedByIdentityId
GO

ALTER TABLE [dbo].[Organization]
    ADD CONSTRAINT CHK_Organization_ChangedByIdentityId_NotEmpty CHECK (ChangedByIdentityId <> '00000000-0000-0000-0000-000000000000');
GO