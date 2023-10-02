/****** Object:  Table [dbo].[Actor]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Actor](
                              [Id] [uniqueidentifier] NOT NULL,
                              [OrganizationId] [uniqueidentifier] NOT NULL,
                              [ActorId] [uniqueidentifier] NULL,
                              [ActorNumber] [nvarchar](50) NOT NULL,
                              [Status] [int] NOT NULL,
                              [Name] [nvarchar](255) NOT NULL,
                              [IsFas] [bit] NOT NULL,
                              CONSTRAINT [PK_Actor] PRIMARY KEY CLUSTERED
                                  (
                                   [Id] ASC
                                      )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ActorContact]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ActorContact](
                                     [Id] [uniqueidentifier] NOT NULL,
                                     [ActorId] [uniqueidentifier] NOT NULL,
                                     [Category] [int] NOT NULL,
                                     [Name] [nvarchar](250) NOT NULL,
                                     [Email] [nvarchar](254) NULL,
                                     [Phone] [nvarchar](15) NULL,
                                     CONSTRAINT [PK_ActorContact] PRIMARY KEY CLUSTERED
                                         (
                                          [Id] ASC
                                             )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
                                     CONSTRAINT [UQ_ActorContact_Categories] UNIQUE NONCLUSTERED
                                         (
                                          [ActorId] ASC,
                                          [Category] ASC
                                             )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ActorSynchronization]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ActorSynchronization](
                                             [Id] [int] IDENTITY(1,1) NOT NULL,
                                             [OrganizationId] [uniqueidentifier] NOT NULL,
                                             [ActorId] [uniqueidentifier] NOT NULL
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[EmailEvent]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EmailEvent](
                                   [Id] [int] IDENTITY(1,1) NOT NULL,
                                   [Email] [nvarchar](255) NOT NULL,
                                   [Created] [datetimeoffset](7) NOT NULL,
                                   [Sent] [datetimeoffset](7) NULL,
                                   [EmailEventType] [int] NOT NULL,
                                   CONSTRAINT [PK_EmailEvent_Id] PRIMARY KEY CLUSTERED
                                       (
                                        [Id] ASC
                                           )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[GridArea]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GridArea](
                                 [Id] [uniqueidentifier] NOT NULL,
                                 [Code] [nvarchar](4) NOT NULL,
                                 [Name] [nvarchar](50) NOT NULL,
                                 [PriceAreaCode] [int] NOT NULL,
                                 [ValidFrom] [datetimeoffset](7) NOT NULL,
                                 [ValidTo] [datetimeoffset](7) NULL,
                                 [FullFlexDate] [datetimeoffset](7) NULL,
                                 CONSTRAINT [PK_GridArea] PRIMARY KEY CLUSTERED
                                     (
                                      [Id] ASC
                                         )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[GridAreaLink]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GridAreaLink](
                                     [Id] [uniqueidentifier] NOT NULL,
                                     [GridAreaId] [uniqueidentifier] NOT NULL,
                                     CONSTRAINT [PK_GridAreaLink] PRIMARY KEY CLUSTERED
                                         (
                                          [Id] ASC
                                             )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[GridAreaMeteringPointType]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GridAreaMeteringPointType](
                                                  [Id] [uniqueidentifier] NOT NULL,
                                                  [MarketRoleGridAreaId] [uniqueidentifier] NOT NULL,
                                                  [MeteringPointTypeId] [int] NOT NULL,
                                                  CONSTRAINT [PK_GridAreaMeteringPointType] PRIMARY KEY CLUSTERED
                                                      (
                                                       [Id] ASC
                                                          )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MarketRole]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MarketRole](
                                   [Id] [uniqueidentifier] NOT NULL,
                                   [ActorId] [uniqueidentifier] NOT NULL,
                                   [Function] [int] NOT NULL,
                                   [Comment] [nvarchar](max) NULL,
                                   CONSTRAINT [PK_MarketRole] PRIMARY KEY CLUSTERED
                                       (
                                        [Id] ASC
                                           )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MarketRoleGridArea]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MarketRoleGridArea](
                                           [Id] [uniqueidentifier] NOT NULL,
                                           [MarketRoleId] [uniqueidentifier] NOT NULL,
                                           [GridAreaId] [uniqueidentifier] NOT NULL,
                                           CONSTRAINT [PK_MarketRoleGridArea] PRIMARY KEY CLUSTERED
                                               (
                                                [Id] ASC
                                                   )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Organization]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Organization](
                                     [Id] [uniqueidentifier] NOT NULL,
                                     [Name] [nvarchar](max) NOT NULL,
                                     [BusinessRegisterIdentifier] [nvarchar](8) NOT NULL,
                                     [Address_StreetName] [nvarchar](250) NULL,
                                     [Address_Number] [nvarchar](15) NULL,
                                     [Address_ZipCode] [nvarchar](15) NULL,
                                     [Address_City] [nvarchar](50) NULL,
                                     [Address_Country] [nvarchar](50) NOT NULL,
                                     [Comment] [nvarchar](max) NULL,
                                     [Status] [int] NOT NULL,
                                     [Domain] [nvarchar](255) NOT NULL,
                                     CONSTRAINT [PK_Organization] PRIMARY KEY CLUSTERED
                                         (
                                          [Id] ASC
                                             )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
                                     CONSTRAINT [UQ_Organization_BusinessRegisterIdentifier] UNIQUE NONCLUSTERED
                                         (
                                          [BusinessRegisterIdentifier] ASC
                                             )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
                                     CONSTRAINT [UQ_Organization_Domain] UNIQUE NONCLUSTERED
                                         (
                                          [Domain] ASC
                                             )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Permission]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Permission](
                                   [Id] [int] NOT NULL,
                                   [Description] [nvarchar](250) NOT NULL,
                                   CONSTRAINT [PK_AccessPermission] PRIMARY KEY CLUSTERED
                                       (
                                        [Id] ASC
                                           )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UniqueActorMarketRoleGridArea]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UniqueActorMarketRoleGridArea](
                                                      [Id] [uniqueidentifier] NOT NULL,
                                                      [ActorId] [uniqueidentifier] NOT NULL,
                                                      [MarketRoleFunction] [int] NOT NULL,
                                                      [GridAreaId] [uniqueidentifier] NOT NULL,
                                                      CONSTRAINT [PK_UniqueActorMarketRoleGridArea] PRIMARY KEY CLUSTERED
                                                          (
                                                           [Id] ASC
                                                              )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
                                                      CONSTRAINT [UQ_UniqueActorMarketRoleGridArea_MarketRoleFunction_GridAreaId] UNIQUE NONCLUSTERED
                                                          (
                                                           [MarketRoleFunction] ASC,
                                                           [GridAreaId] ASC
                                                              )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[User]    Script Date: 19/04/2023 11.05.55 ******/
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
                                     )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserInviteAuditLogEntry]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserInviteAuditLogEntry](
                                                [Id] [int] IDENTITY(1,1) NOT NULL,
                                                [UserId] [uniqueidentifier] NOT NULL,
                                                [ChangedByUserId] [uniqueidentifier] NOT NULL,
                                                [ActorId] [uniqueidentifier] NOT NULL,
                                                [Timestamp] [datetimeoffset](7) NOT NULL,
                                                CONSTRAINT [PK_UserInviteAuditLogEntry] PRIMARY KEY CLUSTERED
                                                    (
                                                     [Id] ASC
                                                        )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserRole]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserRole](
                                 [Id] [uniqueidentifier] NOT NULL,
                                 [Name] [nvarchar](250) NULL,
                                 [Description] [nvarchar](max) NULL,
                                 [Status] [int] NOT NULL,
                                 CONSTRAINT [PK_UserRoleTemplate] PRIMARY KEY CLUSTERED
                                     (
                                      [Id] ASC
                                         )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserRoleAssignment]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserRoleAssignment](
                                           [UserId] [uniqueidentifier] NOT NULL,
                                           [ActorId] [uniqueidentifier] NOT NULL,
                                           [UserRoleId] [uniqueidentifier] NOT NULL,
                                           CONSTRAINT [PK_UserRoleAssignment] PRIMARY KEY CLUSTERED
                                               (
                                                [UserId] ASC,
                                                [ActorId] ASC,
                                                [UserRoleId] ASC
                                                   )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserRoleAssignmentAuditLogEntry]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserRoleAssignmentAuditLogEntry](
                                                        [Id] [int] IDENTITY(1,1) NOT NULL,
                                                        [UserId] [uniqueidentifier] NOT NULL,
                                                        [ActorId] [uniqueidentifier] NOT NULL,
                                                        [UserRoleId] [uniqueidentifier] NOT NULL,
                                                        [ChangedByUserId] [uniqueidentifier] NOT NULL,
                                                        [Timestamp] [datetimeoffset](7) NOT NULL,
                                                        [AssignmentType] [int] NOT NULL,
                                                        CONSTRAINT [PK_UserRoleAssignmentAuditLogEntry] PRIMARY KEY CLUSTERED
                                                            (
                                                             [Id] ASC
                                                                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserRoleEicFunction]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserRoleEicFunction](
                                            [UserRoleId] [uniqueidentifier] NOT NULL,
                                            [EicFunction] [int] NOT NULL,
                                            CONSTRAINT [PK_UserRoleTemplateEicFunction] PRIMARY KEY CLUSTERED
                                                (
                                                 [UserRoleId] ASC,
                                                 [EicFunction] ASC
                                                    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserRolePermission]    Script Date: 19/04/2023 11.05.55 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserRolePermission](
                                           [UserRoleId] [uniqueidentifier] NOT NULL,
                                           [PermissionId] [int] NOT NULL,
                                           CONSTRAINT [PK_UserRoleTemplatePermission] PRIMARY KEY CLUSTERED
                                               (
                                                [UserRoleId] ASC,
                                                [PermissionId] ASC
                                                   )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Actor]  WITH CHECK ADD  CONSTRAINT [FK_OrganizationId_Organization] FOREIGN KEY([OrganizationId])
    REFERENCES [dbo].[Organization] ([Id])
GO
ALTER TABLE [dbo].[Actor] CHECK CONSTRAINT [FK_OrganizationId_Organization]
GO
ALTER TABLE [dbo].[ActorContact]  WITH CHECK ADD  CONSTRAINT [FK_ActorContact_Actor] FOREIGN KEY([ActorId])
    REFERENCES [dbo].[Actor] ([Id])
GO
ALTER TABLE [dbo].[ActorContact] CHECK CONSTRAINT [FK_ActorContact_Actor]
GO
ALTER TABLE [dbo].[GridAreaLink]  WITH CHECK ADD  CONSTRAINT [FK_GridAreaLink_GridAreaId_GridArea] FOREIGN KEY([GridAreaId])
    REFERENCES [dbo].[GridArea] ([Id])
GO
ALTER TABLE [dbo].[GridAreaLink] CHECK CONSTRAINT [FK_GridAreaLink_GridAreaId_GridArea]
GO
ALTER TABLE [dbo].[GridAreaMeteringPointType]  WITH CHECK ADD  CONSTRAINT [FK_MarketRoleGridAreaId_MarketRoleGridArea] FOREIGN KEY([MarketRoleGridAreaId])
    REFERENCES [dbo].[MarketRoleGridArea] ([Id])
GO
ALTER TABLE [dbo].[GridAreaMeteringPointType] CHECK CONSTRAINT [FK_MarketRoleGridAreaId_MarketRoleGridArea]
GO
ALTER TABLE [dbo].[MarketRole]  WITH CHECK ADD  CONSTRAINT [FK_ActorId_Actor_MarketRole] FOREIGN KEY([ActorId])
    REFERENCES [dbo].[Actor] ([Id])
GO
ALTER TABLE [dbo].[MarketRole] CHECK CONSTRAINT [FK_ActorId_Actor_MarketRole]
GO
ALTER TABLE [dbo].[MarketRoleGridArea]  WITH CHECK ADD  CONSTRAINT [FK_MarketRoleGridArea_GridAreaId_GridArea] FOREIGN KEY([GridAreaId])
    REFERENCES [dbo].[GridArea] ([Id])
GO
ALTER TABLE [dbo].[MarketRoleGridArea] CHECK CONSTRAINT [FK_MarketRoleGridArea_GridAreaId_GridArea]
GO
ALTER TABLE [dbo].[MarketRoleGridArea]  WITH CHECK ADD  CONSTRAINT [FK_MarketRoleId_MarketRole] FOREIGN KEY([MarketRoleId])
    REFERENCES [dbo].[MarketRole] ([Id])
GO
ALTER TABLE [dbo].[MarketRoleGridArea] CHECK CONSTRAINT [FK_MarketRoleId_MarketRole]
GO
ALTER TABLE [dbo].[UniqueActorMarketRoleGridArea]  WITH CHECK ADD  CONSTRAINT [FK_UniqueActorMarketRoleGridArea_Actor] FOREIGN KEY([ActorId])
    REFERENCES [dbo].[Actor] ([Id])
GO
ALTER TABLE [dbo].[UniqueActorMarketRoleGridArea] CHECK CONSTRAINT [FK_UniqueActorMarketRoleGridArea_Actor]
GO
ALTER TABLE [dbo].[UniqueActorMarketRoleGridArea]  WITH CHECK ADD  CONSTRAINT [FK_UniqueActorMarketRoleGridArea_GridArea] FOREIGN KEY([GridAreaId])
    REFERENCES [dbo].[GridArea] ([Id])
GO
ALTER TABLE [dbo].[UniqueActorMarketRoleGridArea] CHECK CONSTRAINT [FK_UniqueActorMarketRoleGridArea_GridArea]
GO
ALTER TABLE [dbo].[UserInviteAuditLogEntry]  WITH CHECK ADD  CONSTRAINT [FK_UserInviteAuditLogEntry_ActorId_Actor] FOREIGN KEY([ActorId])
    REFERENCES [dbo].[Actor] ([Id])
GO
ALTER TABLE [dbo].[UserInviteAuditLogEntry] CHECK CONSTRAINT [FK_UserInviteAuditLogEntry_ActorId_Actor]
GO
ALTER TABLE [dbo].[UserInviteAuditLogEntry]  WITH CHECK ADD  CONSTRAINT [FK_UserInviteAuditLogEntry_ChangedByUserId_User] FOREIGN KEY([ChangedByUserId])
    REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[UserInviteAuditLogEntry] CHECK CONSTRAINT [FK_UserInviteAuditLogEntry_ChangedByUserId_User]
GO
ALTER TABLE [dbo].[UserInviteAuditLogEntry]  WITH CHECK ADD  CONSTRAINT [FK_UserInviteAuditLogEntry_UserId_User] FOREIGN KEY([UserId])
    REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[UserInviteAuditLogEntry] CHECK CONSTRAINT [FK_UserInviteAuditLogEntry_UserId_User]
GO
ALTER TABLE [dbo].[UserRoleAssignmentAuditLogEntry]  WITH CHECK ADD  CONSTRAINT [FK_ChangedByUserId_User] FOREIGN KEY([ChangedByUserId])
    REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[UserRoleAssignmentAuditLogEntry] CHECK CONSTRAINT [FK_ChangedByUserId_User]
GO
ALTER TABLE [dbo].[UserRoleAssignmentAuditLogEntry]  WITH CHECK ADD  CONSTRAINT [FK_UserId_User] FOREIGN KEY([UserId])
    REFERENCES [dbo].[User] ([Id])
GO
ALTER TABLE [dbo].[UserRoleAssignmentAuditLogEntry] CHECK CONSTRAINT [FK_UserId_User]
GO
ALTER TABLE [dbo].[UserRoleAssignmentAuditLogEntry]  WITH CHECK ADD  CONSTRAINT [FK_UserRoleId_MarketRoleGridArea] FOREIGN KEY([UserRoleId])
    REFERENCES [dbo].[UserRole] ([Id])
GO
ALTER TABLE [dbo].[UserRoleAssignmentAuditLogEntry] CHECK CONSTRAINT [FK_UserRoleId_MarketRoleGridArea]
GO