IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LittleBobbyTables]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[LittleBobbyTables]
    (
        [Id] [int] IDENTITY(1, 1) NOT NULL,
    )
END
