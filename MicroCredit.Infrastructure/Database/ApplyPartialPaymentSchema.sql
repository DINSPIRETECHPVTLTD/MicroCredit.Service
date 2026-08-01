-- Partial payment schema for LoanSchedulers (Recovery Posting split).
-- Run against dinspire_mcs_dev (or your local DB) in SSMS.
-- Safe to re-run: uses IF NOT EXISTS guards.

SET NOCOUNT ON;

IF COL_LENGTH('dinspire_sa.LoanSchedulers', 'ParentLoanSchedulerId') IS NULL
BEGIN
    ALTER TABLE [dinspire_sa].[LoanSchedulers]
    ADD [ParentLoanSchedulerId] int NULL;
    PRINT 'Added ParentLoanSchedulerId';
END
ELSE
    PRINT 'ParentLoanSchedulerId already exists';

IF COL_LENGTH('dinspire_sa.LoanSchedulers', 'SubInstallmentSequence') IS NULL
BEGIN
    ALTER TABLE [dinspire_sa].[LoanSchedulers]
    ADD [SubInstallmentSequence] int NOT NULL
        CONSTRAINT [DF_LoanSchedulers_SubInstallmentSequence] DEFAULT 0;
    PRINT 'Added SubInstallmentSequence';
END
ELSE
    PRINT 'SubInstallmentSequence already exists';

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_LoanSchedulers_LoanId_InstallmentNo_SubInstallmentSequence'
      AND object_id = OBJECT_ID('[dinspire_sa].[LoanSchedulers]'))
BEGIN
    CREATE INDEX [IX_LoanSchedulers_LoanId_InstallmentNo_SubInstallmentSequence]
    ON [dinspire_sa].[LoanSchedulers] ([LoanId], [InstallmentNo], [SubInstallmentSequence]);
    PRINT 'Created index IX_LoanSchedulers_LoanId_InstallmentNo_SubInstallmentSequence';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_LoanSchedulers_ParentLoanSchedulerId'
      AND object_id = OBJECT_ID('[dinspire_sa].[LoanSchedulers]'))
BEGIN
    CREATE INDEX [IX_LoanSchedulers_ParentLoanSchedulerId]
    ON [dinspire_sa].[LoanSchedulers] ([ParentLoanSchedulerId]);
    PRINT 'Created index IX_LoanSchedulers_ParentLoanSchedulerId';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_LoanSchedulers_LoanSchedulers_ParentLoanSchedulerId')
BEGIN
    ALTER TABLE [dinspire_sa].[LoanSchedulers]
    ADD CONSTRAINT [FK_LoanSchedulers_LoanSchedulers_ParentLoanSchedulerId]
    FOREIGN KEY ([ParentLoanSchedulerId])
    REFERENCES [dinspire_sa].[LoanSchedulers] ([LoanSchedulerId]);
    PRINT 'Created FK_LoanSchedulers_LoanSchedulers_ParentLoanSchedulerId';
END

-- Optional: record EF migration so future dotnet ef database update skips this step
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260729120000_AddLoanSchedulerPartialSubInstallment')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260729120000_AddLoanSchedulerPartialSubInstallment', N'8.0.0');
    PRINT 'Recorded migration in __EFMigrationsHistory';
END

-- Verify
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dinspire_sa'
  AND TABLE_NAME = 'LoanSchedulers'
  AND COLUMN_NAME IN ('ParentLoanSchedulerId', 'SubInstallmentSequence');

PRINT 'Done. Refresh Recovery Posting in the browser.';
