DECLARE @ConstraintName nvarchar(200)
SELECT @ConstraintName = Name FROM SYS.DEFAULT_CONSTRAINTS
WHERE PARENT_OBJECT_ID = OBJECT_ID('OrganizationInfo')
  AND PARENT_COLUMN_ID = (SELECT column_id FROM sys.columns
                          WHERE NAME = N'BusinessRegisterIdentifier'
                            AND object_id = OBJECT_ID(N'OrganizationInfo'))
IF @ConstraintName IS NOT NULL
    EXEC('ALTER TABLE OrganizationInfo DROP CONSTRAINT ' + @ConstraintName)
GO

DECLARE @ConstraintName nvarchar(200)
SELECT @ConstraintName = Name FROM SYS.DEFAULT_CONSTRAINTS
WHERE PARENT_OBJECT_ID = OBJECT_ID('OrganizationInfo')
  AND PARENT_COLUMN_ID = (SELECT column_id FROM sys.columns
                          WHERE NAME = N'Address_Country'
                            AND object_id = OBJECT_ID(N'OrganizationInfo'))
IF @ConstraintName IS NOT NULL
    EXEC('ALTER TABLE OrganizationInfo DROP CONSTRAINT ' + @ConstraintName)

ALTER TABLE [dbo].[OrganizationInfo]
    ADD CONSTRAINT DF_BusiVal DEFAULT N'' FOR BusinessRegisterIdentifier
ALTER TABLE [dbo].[OrganizationInfo]
    ALTER COLUMN BusinessRegisterIdentifier nvarchar(8) NOT NULL
ALTER TABLE [dbo].[OrganizationInfo]
    ADD CONSTRAINT DFCountryVal DEFAULT N'' FOR Address_Country
GO

UPDATE [dbo].[OrganizationInfo]
SET Address_Country = N''
GO

ALTER TABLE [dbo].[OrganizationInfo]
    ALTER COLUMN Address_Country nvarchar(50) NOT NULL
GO