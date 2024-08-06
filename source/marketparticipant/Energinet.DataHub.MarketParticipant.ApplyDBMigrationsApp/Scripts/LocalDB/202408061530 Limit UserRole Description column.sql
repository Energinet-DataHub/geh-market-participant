ALTER TABLE [dbo].[UserRole]
ADD [Description_tmp] [nvarchar](2000) NULL;

UPDATE [dbo].[UserRole]
SET [Description_tmp] = LEFT([Description], 2000);

ALTER TABLE [dbo].[UserRole]
DROP COLUMN [Description];

EXEC sp_rename 'UserRole.Description_tmp', 'Description', 'COLUMN';
