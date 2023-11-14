ALTER TABLE [dbo].[ActorCertificateCredentials]
    ALTER COLUMN [ExpirationDate] [datetimeoffset](7) NOT NULL;

ALTER TABLE [dbo].[ActorCertificateCredentials] ADD

    [Id] [int] IDENTITY(1,1) NOT NULL,

    PeriodStart DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN
    CONSTRAINT DF_ActorCertificateCredentials_PeriodStart DEFAULT SYSUTCDATETIME(),
    PeriodEnd DATETIME2 GENERATED ALWAYS AS ROW END HIDDEN
    CONSTRAINT DF_ActorCertificateCredentials_PeriodEnd   DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME(PeriodStart, PeriodEnd),

    Version             int              NOT NULL DEFAULT 0,
    ChangedByIdentityId uniqueidentifier NOT NULL
    CONSTRAINT DF_ChangedByIdentityId DEFAULT('00000000-FFFF-FFFF-FFFF-000000000000'),
    DeletedByIdentityId uniqueidentifier NULL
	
	PRIMARY KEY (Id);
GO

ALTER TABLE [dbo].[ActorCertificateCredentials]
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.[ActorCertificateCredentialsHistory]));
GO

ALTER TABLE [dbo].[ActorCertificateCredentials]
DROP CONSTRAINT DF_ChangedByIdentityId
GO

ALTER TABLE [dbo].[ActorCertificateCredentials]
    ADD CONSTRAINT CHK_ActorCertificateCredentials_ChangedByIdentityId_NotEmpty CHECK (ChangedByIdentityId <> '00000000-0000-0000-0000-000000000000'),
        CONSTRAINT CHK_ActorCertificateCredentials_DeletedByIdentityId_NotEmpty CHECK (DeletedByIdentityId <> '00000000-0000-0000-0000-000000000000');
GO
