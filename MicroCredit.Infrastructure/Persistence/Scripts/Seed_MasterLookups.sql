/*
  MasterLookups seed — exported from [dinspire_sa].[MasterLookups]
  Generated: 2026-08-18 01:50:55 UTC
  Row count: 30
  No explicit Id values — identity is auto-generated per environment.
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'Payment_Term' AND LookupCode = N'DAY';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'Payment_Term', N'DAY', N'Daily', 1.00, 1, 1, N'Payment term is Daily', SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'Payment_Term' AND LookupCode = N'WK';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'Payment_Term', N'WK', N'Weekly', 7.00, 2, 1, N'Payment term is Weekly', SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'Payment_Term' AND LookupCode = N'MON';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'Payment_Term', N'MON', N'Monthly', 30.00, 3, 1, N'Payment term is Monthly', SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'PaymentMode' AND LookupCode = N'CASH';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'PaymentMode', N'CASH', N'Cash', 0.00, 1, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'PaymentMode' AND LookupCode = N'UPI';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'PaymentMode', N'UPI', N'UPI', 0.00, 2, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'Relationship' AND LookupCode = N'FTH';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'Relationship', N'FTH', N'Father', 0.00, 1, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'Relationship' AND LookupCode = N'MTH';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'Relationship', N'MTH', N'Mother', 0.00, 2, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'Relationship' AND LookupCode = N'SP';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'Relationship', N'SP', N'Spouse', 0.00, 3, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'Relationship' AND LookupCode = N'SON';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'Relationship', N'SON', N'Son', 0.00, 4, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'Relationship' AND LookupCode = N'DAU';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'Relationship', N'DAU', N'Daughter', 0.00, 5, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'Relationship' AND LookupCode = N'BRO';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'Relationship', N'BRO', N'Brother', 0.00, 6, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'Relationship' AND LookupCode = N'SIS';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'Relationship', N'SIS', N'Sister', 0.00, 7, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'AP';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'AP', N'Andhra Pradesh', 0.00, 1, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'AR';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'AR', N'Arunachal Pradesh', 0.00, 2, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'AS';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'AS', N'Assam', 0.00, 3, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'BR';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'BR', N'Bihar', 0.00, 4, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'CG';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'CG', N'Chhattisgarh', 0.00, 5, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'DL';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'DL', N'Delhi', 0.00, 6, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'GA';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'GA', N'Goa', 0.00, 7, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'GJ';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'GJ', N'Gujarat', 0.00, 8, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'HR';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'HR', N'Haryana', 0.00, 9, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'HP';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'HP', N'Himachal Pradesh', 0.00, 10, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'JH';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'JH', N'Jharkhand', 0.00, 11, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'KA';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'KA', N'Karnataka', 0.00, 12, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'KL';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'KL', N'Kerala', 0.00, 13, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'MH';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'MH', N'Maharashtra', 0.00, 14, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'MP';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'MP', N'Madhya Pradesh', 0.00, 15, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'OD';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'OD', N'Odisha', 0.00, 16, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'PB';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'PB', N'Punjab', 0.00, 17, 1, NULL, SYSUTCDATETIME(), N'seed-script');
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
        UpdatedBy = N'seed-script'
    WHERE LookupKey = N'STATE' AND LookupCode = N'TG';
END
ELSE
BEGIN
    INSERT INTO [dinspire_sa].[MasterLookups]
        (LookupKey, LookupCode, LookupValue, NumericValue, SortOrder, IsActive, Description, CreatedOn, CreatedBy)
    VALUES (N'STATE', N'TG', N'Telangana', 0.00, 18, 1, NULL, SYSUTCDATETIME(), N'seed-script');
END

COMMIT TRANSACTION;
GO
