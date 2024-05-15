ALTER TABLE [dbo].[DelegationPeriod] ADD

    PeriodStart DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN
    CONSTRAINT DF_DelegationPeriod_PeriodStart DEFAULT SYSUTCDATETIME(),
    PeriodEnd DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN
    CONSTRAINT DF_DelegationPeriod_PeriodEnd   DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME(PeriodStart, PeriodEnd),
    
    Version             int              NOT NULL DEFAULT 0,
    ChangedByIdentityId uniqueidentifier NOT NULL
GO

ALTER TABLE [dbo].[DelegationPeriod]
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.[DelegationPeriodHistory]));
GO

ALTER TABLE [dbo].[DelegationPeriod]
    ADD CONSTRAINT CHK_DelegationPeriod_ChangedByIdentityId_NotEmpty CHECK (ChangedByIdentityId <> '00000000-0000-0000-0000-000000000000')
GO
