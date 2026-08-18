/*
  MasterLookups seed — exported from dinspire_mcs_dev / dinspire_sa.MasterLookups
  Generated: 2026-08-18 01:46:34 UTC
  Row count: 30

  Run in any environment AFTER EF migrations:
    1. Connect SSMS to target database
    2. Execute this script
    3. Safe to re-run — upserts by (LookupKey, LookupCode)

  Lookup groups included:
    - STATE (18 Indian states)
    - Relationship (7 values)
    - PaymentMode (Cash, UPI)
    - Payment_Term (Daily, Weekly, Monthly)
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'dinspire_sa')
    EXEC(N'CREATE SCHEMA [dinspire_sa]');
GO

BEGIN TRANSACTION;

-- Payment_Term / DAY
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'Payment_Term' AND LookupCode = N'DAY')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Daily',
        NumericValue = 1.00,
        SortOrder = 1,
        IsActive = 1,
        Description = N'Payment term is Daily',
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'Payment_Term' AND LookupCode = N'DAY';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (28, N'Payment_Term', N'DAY', N'Daily', 1.00, 1, 1, N'Payment term is Daily', '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- Payment_Term / WK
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'Payment_Term' AND LookupCode = N'WK')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Weekly',
        NumericValue = 7.00,
        SortOrder = 2,
        IsActive = 1,
        Description = N'Payment term is Weekly',
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'Payment_Term' AND LookupCode = N'WK';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (29, N'Payment_Term', N'WK', N'Weekly', 7.00, 2, 1, N'Payment term is Weekly', '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- Payment_Term / MON
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'Payment_Term' AND LookupCode = N'MON')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Monthly',
        NumericValue = 30.00,
        SortOrder = 3,
        IsActive = 1,
        Description = N'Payment term is Monthly',
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'Payment_Term' AND LookupCode = N'MON';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (30, N'Payment_Term', N'MON', N'Monthly', 30.00, 3, 1, N'Payment term is Monthly', '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- PaymentMode / CASH
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'PaymentMode' AND LookupCode = N'CASH')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Cash',
        NumericValue = 0.00,
        SortOrder = 1,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'PaymentMode' AND LookupCode = N'CASH';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (26, N'PaymentMode', N'CASH', N'Cash', 0.00, 1, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- PaymentMode / UPI
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'PaymentMode' AND LookupCode = N'UPI')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'UPI',
        NumericValue = 0.00,
        SortOrder = 2,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'PaymentMode' AND LookupCode = N'UPI';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (27, N'PaymentMode', N'UPI', N'UPI', 0.00, 2, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- Relationship / FTH
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'Relationship' AND LookupCode = N'FTH')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Father',
        NumericValue = 0.00,
        SortOrder = 1,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'Relationship' AND LookupCode = N'FTH';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (19, N'Relationship', N'FTH', N'Father', 0.00, 1, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- Relationship / MTH
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'Relationship' AND LookupCode = N'MTH')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Mother',
        NumericValue = 0.00,
        SortOrder = 2,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'Relationship' AND LookupCode = N'MTH';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (20, N'Relationship', N'MTH', N'Mother', 0.00, 2, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- Relationship / SP
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'Relationship' AND LookupCode = N'SP')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Spouse',
        NumericValue = 0.00,
        SortOrder = 3,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'Relationship' AND LookupCode = N'SP';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (21, N'Relationship', N'SP', N'Spouse', 0.00, 3, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- Relationship / SON
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'Relationship' AND LookupCode = N'SON')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Son',
        NumericValue = 0.00,
        SortOrder = 4,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'Relationship' AND LookupCode = N'SON';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (22, N'Relationship', N'SON', N'Son', 0.00, 4, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- Relationship / DAU
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'Relationship' AND LookupCode = N'DAU')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Daughter',
        NumericValue = 0.00,
        SortOrder = 5,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'Relationship' AND LookupCode = N'DAU';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (23, N'Relationship', N'DAU', N'Daughter', 0.00, 5, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- Relationship / BRO
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'Relationship' AND LookupCode = N'BRO')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Brother',
        NumericValue = 0.00,
        SortOrder = 6,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'Relationship' AND LookupCode = N'BRO';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (24, N'Relationship', N'BRO', N'Brother', 0.00, 6, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- Relationship / SIS
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'Relationship' AND LookupCode = N'SIS')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Sister',
        NumericValue = 0.00,
        SortOrder = 7,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'Relationship' AND LookupCode = N'SIS';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (25, N'Relationship', N'SIS', N'Sister', 0.00, 7, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / AP
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'AP')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Andhra Pradesh',
        NumericValue = 0.00,
        SortOrder = 1,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'AP';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (1, N'STATE', N'AP', N'Andhra Pradesh', 0.00, 1, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / AR
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'AR')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Arunachal Pradesh',
        NumericValue = 0.00,
        SortOrder = 2,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'AR';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (2, N'STATE', N'AR', N'Arunachal Pradesh', 0.00, 2, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / AS
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'AS')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Assam',
        NumericValue = 0.00,
        SortOrder = 3,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'AS';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (3, N'STATE', N'AS', N'Assam', 0.00, 3, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / BR
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'BR')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Bihar',
        NumericValue = 0.00,
        SortOrder = 4,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'BR';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (4, N'STATE', N'BR', N'Bihar', 0.00, 4, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / CG
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'CG')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Chhattisgarh',
        NumericValue = 0.00,
        SortOrder = 5,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'CG';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (5, N'STATE', N'CG', N'Chhattisgarh', 0.00, 5, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / DL
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'DL')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Delhi',
        NumericValue = 0.00,
        SortOrder = 6,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'DL';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (6, N'STATE', N'DL', N'Delhi', 0.00, 6, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / GA
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'GA')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Goa',
        NumericValue = 0.00,
        SortOrder = 7,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'GA';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (7, N'STATE', N'GA', N'Goa', 0.00, 7, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / GJ
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'GJ')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Gujarat',
        NumericValue = 0.00,
        SortOrder = 8,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'GJ';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (8, N'STATE', N'GJ', N'Gujarat', 0.00, 8, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / HR
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'HR')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Haryana',
        NumericValue = 0.00,
        SortOrder = 9,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'HR';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (9, N'STATE', N'HR', N'Haryana', 0.00, 9, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / HP
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'HP')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Himachal Pradesh',
        NumericValue = 0.00,
        SortOrder = 10,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'HP';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (10, N'STATE', N'HP', N'Himachal Pradesh', 0.00, 10, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / JH
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'JH')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Jharkhand',
        NumericValue = 0.00,
        SortOrder = 11,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'JH';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (11, N'STATE', N'JH', N'Jharkhand', 0.00, 11, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / KA
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'KA')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Karnataka',
        NumericValue = 0.00,
        SortOrder = 12,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'KA';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (12, N'STATE', N'KA', N'Karnataka', 0.00, 12, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / KL
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'KL')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Kerala',
        NumericValue = 0.00,
        SortOrder = 13,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'KL';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (13, N'STATE', N'KL', N'Kerala', 0.00, 13, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / MH
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'MH')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Maharashtra',
        NumericValue = 0.00,
        SortOrder = 14,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'MH';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (14, N'STATE', N'MH', N'Maharashtra', 0.00, 14, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / MP
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'MP')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Madhya Pradesh',
        NumericValue = 0.00,
        SortOrder = 15,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'MP';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (15, N'STATE', N'MP', N'Madhya Pradesh', 0.00, 15, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / OD
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'OD')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Odisha',
        NumericValue = 0.00,
        SortOrder = 16,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'OD';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (16, N'STATE', N'OD', N'Odisha', 0.00, 16, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / PB
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'PB')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Punjab',
        NumericValue = 0.00,
        SortOrder = 17,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'PB';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (17, N'STATE', N'PB', N'Punjab', 0.00, 17, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

-- STATE / TG
IF EXISTS (SELECT 1 FROM [dinspire_sa].[MasterLookups] WHERE LookupKey = N'STATE' AND LookupCode = N'TG')
BEGIN
    UPDATE [dinspire_sa].[MasterLookups] SET
        LookupValue = N'Telangana',
        NumericValue = 0.00,
        SortOrder = 18,
        IsActive = 1,
        Description = NULL,
        UpdatedOn = SYSUTCDATETIME(),
        UpdatedBy = N'export-tool'
    WHERE LookupKey = N'STATE' AND LookupCode = N'TG';
END
ELSE
BEGIN
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] ON;
    INSERT INTO [dinspire_sa].[MasterLookups]
        (Id, LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy, UpdatedOn, UpdatedBy)
    VALUES (18, N'STATE', N'TG', N'Telangana', 0.00, 18, 1, NULL, '2026-04-21 10:23:49.943', N'2', NULL, NULL);
    SET IDENTITY_INSERT [dinspire_sa].[MasterLookups] OFF;
END

COMMIT TRANSACTION;
GO

DECLARE @maxId INT = (SELECT ISNULL(MAX(Id), 0) FROM [dinspire_sa].[MasterLookups]);
DBCC CHECKIDENT ('[dinspire_sa].[MasterLookups]', RESEED, @maxId);
GO
