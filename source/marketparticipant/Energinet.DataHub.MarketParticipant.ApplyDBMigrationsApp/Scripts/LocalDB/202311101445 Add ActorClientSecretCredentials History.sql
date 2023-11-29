ALTER TABLE [dbo].[ActorClientSecretCredentials] ADD

    [Id] [int] IDENTITY(1,1) NOT NULL,

    PeriodStart DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN
    CONSTRAINT DF_ActorClientSecretCredentials_PeriodStart DEFAULT SYSUTCDATETIME(),
    PeriodEnd DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN
    CONSTRAINT DF_ActorClientSecretCredentials_PeriodEnd   DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME(PeriodStart, PeriodEnd),

    Version             int              NOT NULL DEFAULT 0,
    ChangedByIdentityId uniqueidentifier NOT NULL
    CONSTRAINT DF_ChangedByIdentityId DEFAULT('00000000-FFFF-FFFF-FFFF-000000000000'),
    DeletedByIdentityId uniqueidentifier NULL
	
	PRIMARY KEY (Id);
GO

ALTER TABLE [dbo].[ActorClientSecretCredentials]
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.[ActorClientSecretCredentialsHistory]));
GO

ALTER TABLE [dbo].[ActorClientSecretCredentials]
DROP CONSTRAINT DF_ChangedByIdentityId
GO

ALTER TABLE [dbo].[ActorClientSecretCredentials]
    ADD CONSTRAINT CHK_ActorClientSecretCredentials_ChangedByIdentityId_NotEmpty CHECK (ChangedByIdentityId <> '00000000-0000-0000-0000-000000000000'),
        CONSTRAINT CHK_ActorClientSecretCredentials_DeletedByIdentityId_NotEmpty CHECK (DeletedByIdentityId <> '00000000-0000-0000-0000-000000000000');
GO
