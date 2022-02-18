ALTER TABLE [dbo].[OrganizationInfo]
    ADD [ActorId] [uniqueidentifier] NOT NULL DEFAULT (newid())
GO