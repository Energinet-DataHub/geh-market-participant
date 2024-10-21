CREATE TABLE [dbo].[OrganizationDomainEntity](
    [Id]              [uniqueidentifier] NOT NULL,
    [OrganizationId]  [uniqueidentifier] NOT NULL,
    [Domain]          [nvarchar](255)    NOT NULL,

    CONSTRAINT [PK_OrganizationDomainEntity] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT FK_OrganizationDomainEntity_OrganizationId_Organization FOREIGN KEY ([OrganizationId]) REFERENCES [dbo].[Organization]([Id]),
    )
GO
