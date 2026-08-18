/*
  Run in SSMS against SOURCE database (dinspire_mcs_dev).

  1. Connect to source DB
  2. Query -> Results to Text (Ctrl+T)
  3. Execute this script
  4. Save output to Seed_MasterLookups.sql

  Generates upsert SQL without explicit Id / IDENTITY_INSERT.
*/

SET NOCOUNT ON;

DECLARE @schema sysname;
DECLARE @table sysname = N'MasterLookups';

SELECT TOP 1 @schema = s.name
FROM sys.tables t
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE t.name = @table
ORDER BY CASE WHEN s.name = N'dinspire_sa' THEN 0 ELSE 1 END;

IF @schema IS NULL
BEGIN
    RAISERROR('Table MasterLookups was not found.', 16, 1);
    RETURN;
END;

PRINT N'/*';
PRINT N'  MasterLookups seed — exported from ' + DB_NAME() + N'.' + @schema + N'.' + @table;
PRINT N'  Generated: ' + CONVERT(nvarchar(30), SYSUTCDATETIME(), 126) + N' UTC';
PRINT N'  No explicit Id values — identity is auto-generated per environment.';
PRINT N'*/';
PRINT N'';
PRINT N'SET NOCOUNT ON;';
PRINT N'SET XACT_ABORT ON;';
PRINT N'';
PRINT N'IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N''' + @schema + N''')';
PRINT N'    EXEC(N''CREATE SCHEMA [' + @schema + N']'');';
PRINT N'GO';
PRINT N'';
PRINT N'BEGIN TRANSACTION;';
PRINT N'';

DECLARE
    @LookupKey nvarchar(450),
    @LookupCode nvarchar(450),
    @LookupValue nvarchar(max),
    @NumericValue decimal(18, 2),
    @SortOrder int,
    @IsActive bit,
    @Description nvarchar(max),
    @sql nvarchar(max);

DECLARE @q nvarchar(max) = N'
SELECT LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description
FROM [' + @schema + N'].[' + @table + N']
ORDER BY LookupKey, SortOrder, Id;';

DECLARE @rows TABLE (
    LookupKey nvarchar(450),
    LookupCode nvarchar(450),
    LookupValue nvarchar(max),
    NumericValue decimal(18, 2),
    SortOrder int,
    IsActive bit,
    Description nvarchar(max)
);

INSERT INTO @rows EXEC sp_executesql @q;

DECLARE row_cur CURSOR LOCAL FAST_FORWARD FOR SELECT * FROM @rows;

OPEN row_cur;
FETCH NEXT FROM row_cur INTO
    @LookupKey, @LookupCode, @LookupValue, @NumericValue, @SortOrder, @IsActive, @Description;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql =
        N'-- ' + @LookupKey + N' / ' + @LookupCode + CHAR(13) + CHAR(10)
        + N'IF EXISTS (SELECT 1 FROM [' + @schema + N'].[' + @table + N'] WHERE LookupKey = N'''
        + REPLACE(@LookupKey, N'''', N'''''') + N''' AND LookupCode = N'''
        + REPLACE(@LookupCode, N'''', N'''''') + N''')' + CHAR(13) + CHAR(10)
        + N'BEGIN' + CHAR(13) + CHAR(10)
        + N'    UPDATE [' + @schema + N'].[' + @table + N'] SET' + CHAR(13) + CHAR(10)
        + N'        LookupValue = N''' + REPLACE(@LookupValue, N'''', N'''''') + N''',' + CHAR(13) + CHAR(10)
        + N'        NumericValue = ' + ISNULL(CONVERT(nvarchar(30), @NumericValue), N'NULL') + N',' + CHAR(13) + CHAR(10)
        + N'        SortOrder = ' + CONVERT(nvarchar(20), @SortOrder) + N',' + CHAR(13) + CHAR(10)
        + N'        IsActive = ' + CASE WHEN @IsActive = 1 THEN N'1' ELSE N'0' END + N',' + CHAR(13) + CHAR(10)
        + N'        Description = ' + CASE WHEN @Description IS NULL THEN N'NULL' ELSE N'N''' + REPLACE(@Description, N'''', N'''''') + N'''' END + N',' + CHAR(13) + CHAR(10)
        + N'        UpdatedOn = SYSUTCDATETIME(),' + CHAR(13) + CHAR(10)
        + N'        UpdatedBy = N''seed-script''' + CHAR(13) + CHAR(10)
        + N'    WHERE LookupKey = N''' + REPLACE(@LookupKey, N'''', N'''''') + N''' AND LookupCode = N'''
        + REPLACE(@LookupCode, N'''', N'''''') + N''';' + CHAR(13) + CHAR(10)
        + N'END' + CHAR(13) + CHAR(10)
        + N'ELSE' + CHAR(13) + CHAR(10)
        + N'BEGIN' + CHAR(13) + CHAR(10)
        + N'    INSERT INTO [' + @schema + N'].[' + @table + N']' + CHAR(13) + CHAR(10)
        + N'        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)' + CHAR(13) + CHAR(10)
        + N'    VALUES (N''' + REPLACE(@LookupKey, N'''', N'''''') + N''', N'''
        + REPLACE(@LookupCode, N'''', N'''''') + N''', N'''
        + REPLACE(@LookupValue, N'''', N'''''') + N''', '
        + ISNULL(CONVERT(nvarchar(30), @NumericValue), N'NULL') + N', '
        + CONVERT(nvarchar(20), @SortOrder) + N', '
        + CASE WHEN @IsActive = 1 THEN N'1' ELSE N'0' END + N', '
        + CASE WHEN @Description IS NULL THEN N'NULL' ELSE N'N''' + REPLACE(@Description, N'''', N'''''') + N'''' END + N', '
        + N'SYSUTCDATETIME(), N''seed-script'');' + CHAR(13) + CHAR(10)
        + N'END;' + CHAR(13) + CHAR(10);

    PRINT @sql;

    FETCH NEXT FROM row_cur INTO
        @LookupKey, @LookupCode, @LookupValue, @NumericValue, @SortOrder, @IsActive, @Description;
END;

CLOSE row_cur;
DEALLOCATE row_cur;

PRINT N'COMMIT TRANSACTION;';
PRINT N'GO';
PRINT N'-- Row count: ' + CONVERT(nvarchar(20), (SELECT COUNT(*) FROM @rows));
