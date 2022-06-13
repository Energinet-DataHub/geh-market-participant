--Insert new actors
INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name]) VALUES (N'N/A', N'8200000007692', 1, N'DDQ', 1, N'Wholesale_DDQ_1')
INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name]) VALUES (N'N/A', N'8200000007708', 1, N'DDQ', 1, N'Wholesale_DDQ_2')
INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name]) VALUES (N'N/A', N'8200000007715', 1, N'DDK', 1, N'Wholesale_DDK_1')
INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name]) VALUES (N'N/A', N'8200000007722', 1, N'DDK', 1, N'Wholesale_DDK_2')
INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name]) VALUES (N'N/A', N'8200000007739', 1, N'DDM,MDR', 1, N'Wholesale_DDM_MDR_805')
INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name]) VALUES (N'N/A', N'8200000007746', 1, N'DDM,MDR', 1, N'Wholesale_DDM_MDR_806')
INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name]) VALUES (N'N/A', N'44X-00000000004B', 1, N'DDX', 1, N'NBS_Esett')
GO

--Create Grid Areas for Openminds
INSERT [dbo].[GridAreaInfo] ([Id], [RecordId], [Code], [Name], [PriceAreaCode], [Active], [ActorId]) VALUES (N'89801ec1-af12-46d9-b044-05a004a0d46c', 36, N'151', N'Wholesale_GA1', N'DK1', 1, N'N/A')
INSERT [dbo].[GridAreaInfo] ([Id], [RecordId], [Code], [Name], [PriceAreaCode], [Active], [ActorId]) VALUES (N'bd57ad35-fd50-4c23-b485-6087a55cbc8f', 37, N'245', N'Wholesale_GA2', N'DK2', 1, N'N/A')

--Grid Area Links for Openminds
SET IDENTITY_INSERT [dbo].[GridAreaLinkInfo] ON
INSERT [dbo].[GridAreaLinkInfo] ([GridLinkId], [GridAreaId], [RecordId]) VALUES (N'e446e480-2ce6-44f6-9d45-bd891d4b3176', N'89801ec1-af12-46d9-b044-05a004a0d46c', 36)
INSERT [dbo].[GridAreaLinkInfo] ([GridLinkId], [GridAreaId], [RecordId]) VALUES (N'4f43b006-3cdc-4a2a-afa2-3e29b4bbef47', N'bd57ad35-fd50-4c23-b485-6087a55cbc8f', 37)

SET IDENTITY_INSERT [dbo].[GridAreaLinkInfo] OFF
GO