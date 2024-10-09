ALTER TABLE [dbo].[BalanceResponsibilityRelation] ADD
    [ValidToAssignedAt] [datetimeoffset](7) NULL;
GO

UPDATE [dbo].[BalanceResponsibilityRelation]
SET [ValidToAssignedAt] = GETUTCDATE()
WHERE [ValidTo] IS NOT NULL;
GO