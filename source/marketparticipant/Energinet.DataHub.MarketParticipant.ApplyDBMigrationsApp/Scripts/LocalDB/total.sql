-- 202202091216 Create Organization Table.sql
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OrganizationInfo]
(
    [Id]   [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](max)    NOT NULL,
    [Gln]  [nvarchar](50)     NOT NULL
        CONSTRAINT [PK_OrganizationInfo] PRIMARY KEY CLUSTERED
            (
             [Id] ASC
                ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[OrganizationInfo]
    ADD CONSTRAINT [DF_OrganizationInfo_Id] DEFAULT (newid()) FOR [Id]
GO

-- 202202161302 Create GridAreaNew Table.sql
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GridAreaNew]
(
    [Id]   [uniqueidentifier] NOT NULL,
    [Code] [nvarchar](4)      NOT NULL,
    [Name] [nvarchar](50)     NOT NULL,
    CONSTRAINT [PK_GridAreaNew] PRIMARY KEY CLUSTERED
        (
         [Id] ASC
            ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[GridAreaNew]
    ADD CONSTRAINT [DF_GridAreaNew_Id] DEFAULT (newid()) FOR [Id]
GO

-- 202202171430 Create OrganizationRole Table.sql
CREATE TABLE [dbo].[OrganizationRole]
(
    [Id] [uniqueidentifier] NOT NULL,
    [OrganizationId] [uniqueidentifier] NOT NULL,
    [BusinessRole] [int] NOT NULL,
    [Status] [int] NOT NULL,

	CONSTRAINT PK_OrganizationRole PRIMARY KEY ([Id]),
	CONSTRAINT FK_OrganizationId_OrganizationInfo FOREIGN KEY ([OrganizationId]) REFERENCES [dbo].[OrganizationInfo]([Id])
)
GO

-- 202202181115 Add ActorId to Organization Table.sql
ALTER TABLE [dbo].[OrganizationInfo]
    ADD [ActorId] [uniqueidentifier]
GO

-- 202202230930 Add GridAreaId to OrganizationRole Table.sql
ALTER TABLE [dbo].[OrganizationRole]
    ADD [GridAreaId] [uniqueidentifier]
GO

-- 202202231708 Add MeteringTypeToRole Table.sql
CREATE TABLE [dbo].[OrganizationRoleMeteringType]
(
    [Id]                 [uniqueidentifier] NOT NULL,
    [MeteringTypeId]     [int]              NOT NULL,
    [OrganizationRoleId] [uniqueidentifier] NOT NULL
        CONSTRAINT [PK_OrganizationRoleMeteringType] PRIMARY KEY NONCLUSTERED
            (
             [Id] ASC
                ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

-- 202202240930 Create MarketRole Table.sql
CREATE TABLE [dbo].[MarketRole]
(
    [Id] [uniqueidentifier] NOT NULL,
    [OrganizationRoleId] [uniqueidentifier] NOT NULL,
    [Function] [int] NOT NULL,

	CONSTRAINT PK_MarketRole PRIMARY KEY ([Id]),
	CONSTRAINT FK_OrganizationRoleId_OrganizationRole FOREIGN KEY ([OrganizationRoleId]) REFERENCES [dbo].[OrganizationRole]([Id])
)
GO

-- 202202251045 OrganizationInfo GLN unique.sql
ALTER TABLE [dbo].[OrganizationInfo]
ADD CONSTRAINT UNIQUE_GLN UNIQUE (Gln)
GO

-- 202203081530 Refactor Organization.sql
ALTER TABLE [dbo].[OrganizationInfo]
    DROP CONSTRAINT [UNIQUE_GLN]
GO

ALTER TABLE [dbo].[OrganizationInfo]
    DROP COLUMN [Gln], [ActorId]
GO

DROP TABLE [dbo].[OrganizationRoleMeteringType]
GO

DROP TABLE [dbo].[MarketRole]
GO

DROP TABLE [dbo].[OrganizationRole]
GO

CREATE TABLE [dbo].[ActorInfoNew]
(
    [Id]             [uniqueidentifier] NOT NULL,
    [OrganizationId] [uniqueidentifier] NOT NULL,
    [ActorId]        [uniqueidentifier] NOT NULL,
    [Gln]            [nvarchar](50)     NOT NULL,
    [Status]         [int]              NOT NULL,
    [GridAreaId]     [uniqueidentifier] NULL,

    CONSTRAINT PK_ActorInfoNew PRIMARY KEY ([Id]),
    CONSTRAINT FK_OrganizationId_OrganizationInfo FOREIGN KEY ([OrganizationId]) REFERENCES [dbo].[OrganizationInfo]([Id]),
    CONSTRAINT UQ_ActorId UNIQUE ([ActorId])
)
GO

CREATE TABLE [dbo].[MarketRole]
(
    [Id]          [uniqueidentifier] NOT NULL,
    [ActorInfoId] [uniqueidentifier] NOT NULL,
    [Function]    [int]              NOT NULL,

    CONSTRAINT PK_MarketRole PRIMARY KEY ([Id]),
    CONSTRAINT FK_ActorInfoId_ActorInfo_MarketRole FOREIGN KEY ([ActorInfoId]) REFERENCES [dbo].[ActorInfoNew]([Id])
)
GO

CREATE TABLE [dbo].[ActorInfoMeteringType]
(
    [Id]             [uniqueidentifier] NOT NULL,
    [MeteringTypeId] [int]              NOT NULL,
    [ActorInfoId]    [uniqueidentifier] NOT NULL,

    CONSTRAINT PK_ActorInfoMeteringType PRIMARY KEY ([Id]),
    CONSTRAINT FK_ActorInfoId_ActorInfo_MeteringType FOREIGN KEY ([ActorInfoId]) REFERENCES [dbo].[ActorInfoNew]([Id])
)
GO

-- 202203091342 Create Contacts Table.sql
CREATE TABLE [dbo].[Contact]
(
    [Id]       [uniqueidentifier] NOT NULL,
    [Category] [int]              NOT NULL,
    [Name]     [nvarchar](250)    NOT NULL,
    [Email]    [nvarchar](250)    NOT NULL,
    [Phone]    [nvarchar](250)    NOT NULL,

    CONSTRAINT PK_Contact PRIMARY KEY ([Id]),
)
GO

-- 202203291000 Contacts Phone Optional.sql
ALTER TABLE [dbo].[Contact]
ALTER COLUMN [Email] [nvarchar](254)
GO

ALTER TABLE [dbo].[Contact]
ALTER COLUMN [Phone] [nvarchar](15) NULL
GO

ALTER TABLE [dbo].[Contact]
ADD [OrganizationId] [uniqueidentifier] NOT NULL
CONSTRAINT FK_OrganizationId_Contact_OrganizationInfo FOREIGN KEY ([OrganizationId]) REFERENCES [dbo].[OrganizationInfo]([Id])
CONSTRAINT UQ_Categories UNIQUE ([OrganizationId], [Category])
GO


-- 202203311033 Add Address And Cvr to Organization.sql
ALTER TABLE [dbo].[OrganizationInfo]
    ADD BusinessRegisterIdentifier nvarchar(8) DEFAULT N'',
        Address_StreetName nvarchar(250) NULL,
        Address_Number nvarchar(15) NULL,
        Address_ZipCode nvarchar(15) NULL,
        Address_City nvarchar(50) NULL,
        Address_Country nvarchar(50) DEFAULT N'DK'
GO

-- 202204051318 Alter Address Columns for Organization.sql
UPDATE [dbo].[OrganizationInfo]
SET Address_Country = N'DK'
GO

UPDATE [dbo].[OrganizationInfo]
SET BusinessRegisterIdentifier = N''
GO

ALTER TABLE [dbo].[OrganizationInfo]
    ALTER COLUMN Address_Country nvarchar(50) NOT NULL

ALTER TABLE [dbo].[OrganizationInfo]
    ALTER COLUMN BusinessRegisterIdentifier nvarchar(8) NOT NULL
GO

-- 202204051445 Add PriceAreaCode to GridAreaNew.sql
ALTER TABLE [dbo].[GridAreaNew]
    ADD
        PriceAreaCode int not null default 1
GO

-- 202204062145 Add Comment to Organization.sql
ALTER TABLE [dbo].[OrganizationInfo]
    ADD
        Comment nvarchar(max)
GO

-- 202204221002 Create GridAreaLinkNew Table.sql
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GridAreaLinkNew](
     [Id] [uniqueidentifier] NOT NULL,
     [GridAreaId] [uniqueidentifier] NOT NULL,
     CONSTRAINT [PK_GridAreaLinkNew] PRIMARY KEY CLUSTERED
         (
          [Id] ASC
             )
         WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],
     CONSTRAINT FK_GridAreaId_GridAreaNew FOREIGN KEY ([GridAreaId]) REFERENCES [dbo].[GridAreaNew]([Id])

) ON [PRIMARY]
GO

-- 202205180900 Create GridAreaActorInfoLink Table.sql
ALTER TABLE [dbo].[ActorInfoNew]
    DROP COLUMN [GridAreaId]
GO

CREATE TABLE [dbo].[GridAreaActorInfoLink]
(
    [Id]             [uniqueidentifier] NOT NULL,
    [GridAreaId]     [uniqueidentifier] NOT NULL,
    [ActorInfoId]    [uniqueidentifier] NOT NULL,

    CONSTRAINT PK_GridAreaActorInfoLink PRIMARY KEY ([Id]),
    CONSTRAINT FK_GridAreaId_GridArea_GridAreaActorInfoLink FOREIGN KEY ([GridAreaId]) REFERENCES [dbo].[GridAreaNew]([Id]),
    CONSTRAINT FK_ActorInfoId_ActorInfo_GridAreaActorInfoLink FOREIGN KEY ([ActorInfoId]) REFERENCES [dbo].[ActorInfoNew]([Id])
)
GO


-- 202205201000 Optional ActorId.sql
ALTER TABLE [dbo].[ActorInfoNew]
    ALTER COLUMN [ActorId] [uniqueidentifier] NULL
GO

ALTER TABLE [dbo].[ActorInfoNew]
    DROP CONSTRAINT [UQ_ActorId]
GO


-- 202205231000 Create ActorContact Table.sql
CREATE TABLE [dbo].[ActorContact]
(
    [Id]                [uniqueidentifier] NOT NULL,
    [ActorId]           [uniqueidentifier] NOT NULL,
    [Category]          [int]              NOT NULL,
    [Name]              [nvarchar](250)    NOT NULL,
    [Email]             [nvarchar](250)    NOT NULL,
    [Phone]             [nvarchar](250)    NOT NULL,

    CONSTRAINT PK_ActorContact PRIMARY KEY ([Id]),
    CONSTRAINT FK_ActorContact_Actor FOREIGN KEY ([ActorId]) REFERENCES [dbo].[ActorInfoNew]([Id]),
)
GO

-- 202206011312 Actor Contacts Phone Optional.sql
ALTER TABLE [dbo].[ActorContact]
ALTER COLUMN [Email] [nvarchar](254)
GO

ALTER TABLE [dbo].[ActorContact]
ALTER COLUMN [Phone] [nvarchar](15) NULL
GO

ALTER TABLE [dbo].[ActorContact]
ADD CONSTRAINT UQ_ActorContact_Categories UNIQUE ([ActorId], [Category])
GO


-- 202206131236 Rename Gln To ActorNumber.sql
EXEC sp_rename 'ActorInfoNew.Gln', 'ActorNumber', 'COLUMN';
GO   

-- 202206171100 Create MarketRoleGridArea Table.sql
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MarketRoleGridArea]
(
    [Id]                [uniqueidentifier] NOT NULL,
    [MarketRoleId]      [uniqueidentifier] NOT NULL,
    [GridAreaId]        [uniqueidentifier] NOT NULL,

    CONSTRAINT [PK_MarketRoleGridArea] PRIMARY KEY CLUSTERED ([Id] ASC) 
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],

    CONSTRAINT FK_MarketRoleId_MarketRole FOREIGN KEY ([MarketRoleId]) REFERENCES [dbo].[MarketRole]([Id]),
    CONSTRAINT FK_GridAreaId_GridArea FOREIGN KEY ([GridAreaId]) REFERENCES [dbo].[GridAreaNew]([Id])

    )
GO

-- 202206171101 Create GridAreaMeteringPointType Table.sql
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GridAreaMeteringPointType]
(
    [Id]                    [uniqueidentifier] NOT NULL,
    [MarketRoleGridAreaId]  [uniqueidentifier] NOT NULL,
    [MeteringPointTypeId]   [INT] NOT NULL,

    CONSTRAINT [PK_GridAreaMeteringPointType] PRIMARY KEY CLUSTERED ([Id] ASC) 
        WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],

    CONSTRAINT FK_MarketRoleGridAreaId_MarketRoleGridArea FOREIGN KEY ([MarketRoleGridAreaId]) REFERENCES [dbo].[MarketRoleGridArea]([Id])
)
GO

-- 202206301238 Drop old actor gridarea and mp tables.sql

DROP TABLE [dbo].[ActorInfoMeteringType]

DROP TABLE [dbo].[GridAreaActorInfoLink]

DROP TABLE [dbo].[Contact]

-- 202207041245 Create UniqueActorMarketRoleGridArea.sql
CREATE TABLE [dbo].[UniqueActorMarketRoleGridArea]
(
    [Id]                    [uniqueidentifier] NOT NULL,
    [ActorId]               [uniqueidentifier] NOT NULL,
    [MarketRoleFunction]    [int]              NOT NULL,
    [GridAreaId]            [uniqueidentifier] NOT NULL,

    CONSTRAINT PK_UniqueActorMarketRoleGridArea PRIMARY KEY ([Id]),
    CONSTRAINT FK_UniqueActorMarketRoleGridArea_Actor FOREIGN KEY ([ActorId]) REFERENCES [dbo].[ActorInfoNew]([Id]),
    CONSTRAINT FK_UniqueActorMarketRoleGridArea_GridArea FOREIGN KEY ([GridAreaId]) REFERENCES [dbo].[GridAreaNew]([Id]),
    CONSTRAINT UQ_UniqueActorMarketRoleGridArea_MarketRoleFunction_GridAreaId UNIQUE ([MarketRoleFunction], [GridAreaId])
)
GO

-- 202207151250 OrganizationInfo BusinessRegisterIdetifier unique.sql
ALTER TABLE [dbo].[OrganizationInfo]
ADD CONSTRAINT UQ_OrganizationInfo_BusinessRegisterIdentifier UNIQUE (BusinessRegisterIdentifier)
GO

-- 202207250930 Add Name to Actor.sql
ALTER TABLE [dbo].[ActorInfoNew]
    ADD [Name] nvarchar(255) NOT NULL DEFAULT('')
GO

-- 202207250945 Add Status to Organization.sql
ALTER TABLE [dbo].[OrganizationInfo]
    ADD
        [Status] INT NOT NULL DEFAULT(1)
GO

-- 202208221400 Add ValidFrom and ValidTo to GridAreaNew.sql
ALTER TABLE [dbo].[GridAreaNew]
    ADD [ValidFrom] DATETIMEOFFSET NOT NULL DEFAULT ('0001-01-01T00:00:00+00:00')

ALTER TABLE [dbo].[GridAreaNew]
    ADD [ValidTo] DATETIMEOFFSET NULL
GO

-- 202209011008 Add FullFlexDate to GridAreaNew.sql
ALTER TABLE [dbo].[GridAreaNew]
    ADD [FullFlexDate] DATETIMEOFFSET NULL
GO

-- 202209011545 GridAreaAuditLog tabel.sql
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GridAreaAuditLogEntry]
(
    [Id]            [int] IDENTITY(1, 1) NOT NULL,
    [UserId]        [uniqueidentifier]   NOT NULL,
    [Timestamp]     [datetimeoffset]     NOT NULL,
    [Field]         [int]                NOT NULL,
    [OldValue]      [nvarchar](MAX)      NOT NULL,
    [NewValue]      [nvarchar](MAX)      NOT NULL,
    [GridAreaId]    [uniqueidentifier]   NOT NULL
        CONSTRAINT [PK_AuditLog_Id] PRIMARY KEY CLUSTERED ([Id] ASC) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],
        CONSTRAINT FK_GridAreaAuditLogEntry_GridAreaId_GridAreaNew FOREIGN KEY ([GridAreaId]) REFERENCES [dbo].[GridAreaNew]([Id])
) ON [PRIMARY]
GO


-- 202209191415 Delete deleted actors.sql
DELETE x
FROM [dbo].[ActorContact] x
JOIN [dbo].[ActorInfoNew] a ON x.ActorId = a.Id AND a.Status = 5

DELETE z
FROM [dbo].[GridAreaMeteringPointType] z
JOIN [dbo].[MarketRoleGridArea] y ON z.MarketRoleGridAreaId = y.Id
JOIN [dbo].[MarketRole] x ON y.MarketRoleId = x.Id
JOIN [dbo].[ActorInfoNew] a ON x.ActorInfoId = a.Id AND a.Status = 5

DELETE y
FROM [dbo].[MarketRoleGridArea] y
JOIN [dbo].[MarketRole] x ON y.MarketRoleId = x.Id
JOIN [dbo].[ActorInfoNew] a ON x.ActorInfoId = a.Id AND a.Status = 5

DELETE x
FROM [dbo].[MarketRole] x
JOIN [dbo].[ActorInfoNew] a ON x.ActorInfoId = a.Id AND a.Status = 5

DELETE x
FROM [dbo].[UniqueActorMarketRoleGridArea] x
JOIN [dbo].[ActorInfoNew] a ON x.ActorId = a.Id AND a.Status = 5

DELETE a
FROM [dbo].[ActorInfoNew] a WHERE a.Status = 5

GO


-- 202211031500 Create ActorSynchronization Table.sql
CREATE TABLE [dbo].[ActorSynchronization]
(
    [Id]             [int] IDENTITY(1, 1),
    [OrganizationId] [uniqueidentifier] NOT NULL,
    [ActorId]        [uniqueidentifier] NOT NULL
)
GO


-- 202211041100 Add Comment to MarketRole.sql
ALTER TABLE [dbo].[MarketRole]
    ADD [Comment] nvarchar(max) NULL DEFAULT('')
GO

-- 202211211022 Add User Table.sql
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[User](
    [Id] [uniqueidentifier] NOT NULL,
    [ExternalId] [uniqueidentifier] NOT NULL,
    [Email] [nvarchar](250) NULL,
    CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
    GO

-- 202211211238 Add UserRoleAssignment Table.sql
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UserRoleAssignment](
    [UserId] [uniqueidentifier] NOT NULL,
    [ActorId] [uniqueidentifier] NOT NULL,
    [TemplateId] [uniqueidentifier] NOT NULL,
     CONSTRAINT [PK_UserRoleAssignment] PRIMARY KEY CLUSTERED
    (
    [UserId] ASC,
    [ActorId] ASC,
[TemplateId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
    GO

-- 202211211238 Add UserRoleTemplate Table.sql
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UserRoleTemplate](
    [Id] [uniqueidentifier] NOT NULL,
    [Name] [nvarchar](250) NULL,
    CONSTRAINT [PK_UserRoleTemplate] PRIMARY KEY CLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
    GO

-- 202211211238 Add UserRoleTemplateEicFunction Table.sql
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UserRoleTemplateEicFunction](
    [UserRoleTemplateId] [uniqueidentifier] NOT NULL,
    [EicFunction] [int] NOT NULL,
     CONSTRAINT [PK_UserRoleTemplateEicFunction] PRIMARY KEY CLUSTERED
    (
    [UserRoleTemplateId] ASC,
[EicFunction] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
    GO

-- 202211211238 Add UserRoleTemplatePermission Table.sql
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UserRoleTemplatePermission](
    [UserRoleTemplateId] [uniqueidentifier] NOT NULL,
    [PermissionId] [int] NOT NULL,
     CONSTRAINT [PK_UserRoleTemplatePermission] PRIMARY KEY CLUSTERED
    (
    [UserRoleTemplateId] ASC,
[PermissionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
    GO

-- 202211301515 Add IsFas to Actor.sql
ALTER TABLE [dbo].[ActorInfoNew]
    ADD [IsFas] bit NOT NULL DEFAULT(0)
GO

-- 202212160800 Remove 'template' from tables.sql
EXEC sp_rename 'dbo.UserRoleAssignment.TemplateId', 'UserRoleId', 'COLUMN';
EXEC sp_rename 'dbo.UserRoleTemplateEicFunction.UserRoleTemplateId', 'UserRoleId', 'COLUMN';
EXEC sp_rename 'dbo.UserRoleTemplatePermission.UserRoleTemplateId', 'UserRoleId', 'COLUMN';

EXEC sp_rename 'dbo.UserRoleTemplateEicFunction', 'UserRoleEicFunction';
EXEC sp_rename 'dbo.UserRoleTemplatePermission', 'UserRolePermission';
EXEC sp_rename 'dbo.UserRoleTemplate', 'UserRole';


-- 202212201300 Add UserRoleAssignmentAuditLog Table.sql
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UserRoleAssignmentAuditLogEntry](
    [Id] [INT] NOT NULL IDENTITY(1, 1),
    [UserId] [uniqueidentifier] NOT NULL,
    [ActorId] [uniqueidentifier] NOT NULL,
    [UserRoleId] [uniqueidentifier] NOT NULL,
    [ChangedByUserId] [uniqueidentifier] NOT NULL,
    [Timestamp] [DATETIMEOFFSET] NOT NULL,
    [AssignmentType] [INT] NOT NULL,

    CONSTRAINT [PK_UserRoleAssignmentAuditLogEntry] PRIMARY KEY CLUSTERED ([Id] ASC)
    WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],

    CONSTRAINT FK_UserId_User FOREIGN KEY ([UserId]) REFERENCES [dbo].[User]([Id]),
    CONSTRAINT FK_ChangedByUserId_User FOREIGN KEY ([ChangedByUserId]) REFERENCES [dbo].[User]([Id]),
    CONSTRAINT FK_UserRoleId_MarketRoleGridArea FOREIGN KEY ([UserRoleId]) REFERENCES [dbo].[UserRole]([Id])
)
GO

-- 202301031518 Add description and status to UserRole.sql
ALTER TABLE [dbo].[UserRole]
    ADD [Description] nvarchar(max) NULL DEFAULT('')

ALTER TABLE [dbo].[UserRole]
    ADD [Status] [INT] NOT NULL DEFAULT(0)
GO

-- 202301031518 Remove default from status on UserRole.sql
ALTER TABLE [dbo].[UserRole]
ALTER COLUMN [Status] [INT] NOT NULL
GO

-- 202301161014 Add UserRoleAuditLog Table.sql
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UserRoleAuditLogEntry](
    [Id] [INT] NOT NULL IDENTITY(1, 1),
    [UserRoleId] [uniqueidentifier] NOT NULL,
    [ChangedByUserId] [uniqueidentifier] NOT NULL,
    [UserRoleChangeType] [INT] NOT NULL,
    [ChangeDescriptionJson] NVARCHAR(MAX) NULL,
    [Timestamp] [DATETIMEOFFSET] NOT NULL,

    CONSTRAINT [PK_UserRoleAuditLogEntry] PRIMARY KEY CLUSTERED ([Id] ASC)
    WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],

    CONSTRAINT FK_UserRoleAuditLogEntry_ChangedByUserId_User FOREIGN KEY ([ChangedByUserId]) REFERENCES [dbo].[User]([Id]),
    CONSTRAINT FK_UserRoleAuditLogEntry_UserRoleId_UserRole FOREIGN KEY ([UserRoleId]) REFERENCES [dbo].[UserRole]([Id])
)
GO

-- 202301311456 Add Permission Table.sql
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Permission](
    [Id] [INT] NOT NULL,
    [Description] [nvarchar](250) NOT NULL,
    CONSTRAINT [PK_AccessPermission] PRIMARY KEY CLUSTERED
(
[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
    GO

-- 202301311457 Add PermissionToMarketRole Table.sql
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PermissionEicFunction](
    [Id] [uniqueidentifier] NOT NULL,
    [PermissionId] [INT] NOT NULL,
    [EicFunction] [INT] NOT NULL,
    CONSTRAINT [PK_PermissionEicFunction] PRIMARY KEY CLUSTERED
    (
        [Id] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
    GO

ALTER TABLE [dbo].[PermissionEicFunction]  WITH CHECK ADD  CONSTRAINT [FK_PermissionEicFunction_Permission] FOREIGN KEY([PermissionId])
    REFERENCES [dbo].[Permission] ([Id])
    ON UPDATE CASCADE
       ON DELETE CASCADE
GO

ALTER TABLE [dbo].[PermissionEicFunction] CHECK CONSTRAINT [FK_PermissionEicFunction_Permission]
    GO

-- 202302021358 Create Email Event table.sql
SET ANSI_NULLS ON
GO
    
SET QUOTED_IDENTIFIER ON
GO
    
CREATE TABLE [dbo].[EmailEvent]
(
    [Id]             [int] IDENTITY(1, 1)   NOT NULL,
    [Email]          [nvarchar](255)        NOT NULL,
    [Created]        [datetimeoffset]       NOT NULL,
    [Sent]           [datetimeoffset]       NULL,
    [EmailEventType] [int]                  NOT NULL
    
    CONSTRAINT [PK_EmailEvent_Id] PRIMARY KEY CLUSTERED([Id] ASC)
    WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


-- 202302071515 OrganizationInfo Domain.sql
ALTER TABLE [dbo].[OrganizationInfo]
    ADD [Domain] nvarchar(255) NOT NULL DEFAULT('')
GO

-- 202302130900 OrganizationInfo Domain unique index.sql
ALTER TABLE [dbo].[OrganizationInfo]
ADD CONSTRAINT UQ_OrganizationInfo_Domain UNIQUE (Domain);

-- 202302201100 Add UserInviteAuditLog Table.sql
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UserInviteAuditLogEntry](
    [Id] [INT] NOT NULL IDENTITY(1, 1),
    [UserId] [uniqueidentifier] NOT NULL,
    [ChangedByUserId] [uniqueidentifier] NOT NULL,
    [ActorId] [uniqueidentifier] NOT NULL,
    [Timestamp] [DATETIMEOFFSET] NOT NULL,

    CONSTRAINT [PK_UserInviteAuditLogEntry] PRIMARY KEY CLUSTERED ([Id] ASC)
    WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],

    CONSTRAINT FK_UserInviteAuditLogEntry_ChangedByUserId_User FOREIGN KEY ([ChangedByUserId]) REFERENCES [dbo].[User]([Id]),
    CONSTRAINT FK_UserInviteAuditLogEntry_UserId_User FOREIGN KEY ([UserId]) REFERENCES [dbo].[User]([Id]),
    CONSTRAINT FK_UserInviteAuditLogEntry_ActorId_Actor FOREIGN KEY ([ActorId]) REFERENCES [dbo].[ActorInfoNew]([Id]),
    )
GO

-- 202303021132 Add Permission Created.sql
ALTER TABLE [dbo].[Permission]
    ADD [Created] [DATETIMEOFFSET] NULL
GO

UPDATE [dbo].[Permission] SET [Created] = GETUTCDATE()
GO

ALTER TABLE [dbo].[Permission] ALTER COLUMN [Created] [DATETIMEOFFSET] NOT NULL;
GO

-- 202303030953 Add PermissionAuditLog Table.sql
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PermissionAuditLogEntry](
    [EntryId] [INT] NOT NULL IDENTITY(1, 1),
    [PermissionId] [INT] NOT NULL,
    [ChangedByUserId] [uniqueidentifier] NOT NULL,
    [PermissionChangeType] [INT] NOT NULL,
    [Timestamp] [DATETIMEOFFSET] NOT NULL,

    CONSTRAINT [PK_PermissionAuditLogEntry] PRIMARY KEY CLUSTERED ([EntryId] ASC)
    WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY],
    
    CONSTRAINT FK_PermissionAuditLogEntry_PermissionId_Permission FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permission]([Id]),
    CONSTRAINT FK_PermissionAuditLogEntry_ChangedByUserId_User FOREIGN KEY ([ChangedByUserId]) REFERENCES [dbo].[User]([Id]),
    )
GO

-- 202303141130 Remove PermissionEicFunction and Permission.sql
ALTER TABLE [dbo].[PermissionAuditLogEntry]
DROP CONSTRAINT [FK_PermissionAuditLogEntry_PermissionId_Permission]
GO

ALTER TABLE [dbo].[Permission]
DROP COLUMN [Created]
GO

DROP Table [dbo].[PermissionEicFunction]
GO


-- 202304141500 Add Value to PermissionAuditLogEntry table.sql
ALTER TABLE [dbo].[PermissionAuditLogEntry]
    ADD [Value] [nvarchar](max) NOT NULL DEFAULT ''
GO


