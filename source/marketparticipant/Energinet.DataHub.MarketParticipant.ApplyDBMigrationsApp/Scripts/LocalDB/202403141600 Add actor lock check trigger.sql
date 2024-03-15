CREATE TABLE [dbo].[Lock]
(
    [Id] uniqueidentifier NOT NULL PRIMARY KEY,
)
GO

ALTER TABLE [dbo].[Actor]
    ADD [LockId] uniqueidentifier NULL

ALTER TABLE [dbo].[Actor]
    ADD CONSTRAINT FK_LockId
    FOREIGN KEY (LockId)
    REFERENCES Lock(Id);
GO
