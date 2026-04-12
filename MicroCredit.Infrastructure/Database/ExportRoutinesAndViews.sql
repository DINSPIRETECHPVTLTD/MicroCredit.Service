-- Run in SSMS (or sqlcmd) with results directed to a file to capture CREATE scripts.
-- If OBJECT_DEFINITION returns NULL, grant VIEW DEFINITION (see GrantViewDefinitionForScaffolding.sql).

SET NOCOUNT ON;

PRINT N'-- ========== VIEWS ==========';
DECLARE @vschema sysname, @vname sysname, @vdef nvarchar(max);
DECLARE v CURSOR LOCAL FAST_FORWARD FOR
SELECT s.name, v.name
FROM sys.views v
JOIN sys.schemas s ON v.schema_id = s.schema_id
WHERE v.is_ms_shipped = 0
ORDER BY s.name, v.name;

OPEN v;
FETCH NEXT FROM v INTO @vschema, @vname;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @vdef = OBJECT_DEFINITION(OBJECT_ID(QUOTENAME(@vschema) + N'.' + QUOTENAME(@vname)));
    PRINT N'GO';
    PRINT N'-- View ' + QUOTENAME(@vschema) + N'.' + QUOTENAME(@vname);
    PRINT ISNULL(@vdef, N'-- (definition not visible; grant VIEW DEFINITION)');
    FETCH NEXT FROM v INTO @vschema, @vname;
END
CLOSE v;
DEALLOCATE v;

PRINT N'-- ========== PROCEDURES ==========';
DECLARE @pschema sysname, @pname sysname, @pdef nvarchar(max);
DECLARE p CURSOR LOCAL FAST_FORWARD FOR
SELECT s.name, pr.name
FROM sys.procedures pr
JOIN sys.schemas s ON pr.schema_id = s.schema_id
WHERE pr.is_ms_shipped = 0
ORDER BY s.name, pr.name;

OPEN p;
FETCH NEXT FROM p INTO @pschema, @pname;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @pdef = OBJECT_DEFINITION(OBJECT_ID(QUOTENAME(@pschema) + N'.' + QUOTENAME(@pname)));
    PRINT N'GO';
    PRINT N'-- Procedure ' + QUOTENAME(@pschema) + N'.' + QUOTENAME(@pname);
    PRINT ISNULL(@pdef, N'-- (definition not visible; grant VIEW DEFINITION)');
    FETCH NEXT FROM p INTO @pschema, @pname;
END
CLOSE p;
DEALLOCATE p;
