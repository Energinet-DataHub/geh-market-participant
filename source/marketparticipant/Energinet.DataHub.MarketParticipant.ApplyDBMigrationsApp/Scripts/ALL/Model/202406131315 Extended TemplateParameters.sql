DECLARE @ConstraintName NVARCHAR(255);
DECLARE @SQL NVARCHAR(MAX);

SELECT 
    @ConstraintName = df.name
FROM 
    sys.default_constraints df
JOIN 
    sys.columns clmns ON df.parent_object_id = clmns.object_id AND df.parent_column_id = clmns.column_id
JOIN 
    sys.tables tbls ON df.parent_object_id = tbls.object_id
WHERE 
    tbls.name = 'EmailEvent' AND clmns.name = 'TemplateParameters';

SET @SQL = 'ALTER TABLE [dbo].[EmailEvent] DROP CONSTRAINT ' + QUOTENAME(@ConstraintName);
EXEC sp_executesql @SQL;

ALTER TABLE [dbo].[EmailEvent]
    ALTER COLUMN [TemplateParameters] nvarchar(max) NOT NULL
GO
