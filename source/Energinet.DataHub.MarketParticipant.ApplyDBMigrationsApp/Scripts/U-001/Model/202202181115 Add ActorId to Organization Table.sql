ALTER TABLE [dbo].[OrganizationInfo]
    ADD [ActorId] [uniqueidentifier] DEFAULT (newid())
GO