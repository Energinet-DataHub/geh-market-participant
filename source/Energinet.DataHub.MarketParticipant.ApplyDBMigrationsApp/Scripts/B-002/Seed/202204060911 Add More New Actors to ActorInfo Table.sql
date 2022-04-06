--Add Role to previous ModStrøm Actor
UPDATE ActorInfo
SET Roles = 'DDK,DDQ'
WHERE Id = 'add5bf67-9f24-4ebb-94a6-22dfc1e465cf'
GO

--Insert new actors
INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name]) VALUES (N'd5581195-92c0-400c-86b3-6d5915cf2536', N'8200000007661', 1, N'DDM', 1, N'Openminds ApS')
INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name]) VALUES (N'a4697b25-cac5-45f9-8f32-6465c7829229', N'8200000007678', 1, N'MDR', 1, N'Openminds ApS')
INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name]) VALUES (N'5507d87b-bec5-45bb-add1-5f5c661dbefe', N'5790002295607', 1, N'DDQ', 1, N'SEAS-NVE Strømmen (NEMO)')
INSERT [dbo].[ActorInfo] ([Id], [IdentificationNumber], [IdentificationType], [Roles], [Active], [Name]) VALUES (N'eb99aad6-6fa0-4925-85f5-c8269c92fdb8', N'5790001095383', 1, N'MDR', 1, N'SEAS-NVE Strømmen (SAP) - Måledataansvarlig')
GO

-- INSERT [dbo].[GridAreaInfo] ([Id], [RecordId], [Code], [Name], [PriceAreaCode], [Active], [ActorId]) VALUES (N'8a8d565b-f122-445c-ba28-eeec3afae608', 33, N'853', N'Nakskov', N'DK1', 1, N'3437964d-ebaa-448a-abfd-eb589228f0e9')
--
-- --Grid Area Links
-- SET IDENTITY_INSERT [dbo].[GridAreaLinkInfo] ON
-- INSERT [dbo].[GridAreaLinkInfo] ([GridLinkId], [GridAreaId], [RecordId]) VALUES (N'073fb26a-b29d-4083-9a78-3b346e3db547', N'8a8d565b-f122-445c-ba28-eeec3afae608', 33)
-- SET IDENTITY_INSERT [dbo].[GridAreaLinkInfo] OFF
-- GO