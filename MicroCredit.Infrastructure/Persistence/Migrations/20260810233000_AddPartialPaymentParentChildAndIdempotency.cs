using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroCredit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public class AddPartialPaymentParentChildAndIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Columns may already exist on target DBs (Phase 0). Create only when missing.
            // Filtered indexes require QUOTED_IDENTIFIER ON (sqlcmd defaults can be OFF).
            migrationBuilder.Sql(@"
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH(N'dinspire_sa.LoanSchedulers', N'ParentLoanSchedulerId') IS NULL
BEGIN
    ALTER TABLE [dinspire_sa].[LoanSchedulers] ADD [ParentLoanSchedulerId] INT NULL;
END

IF COL_LENGTH(N'dinspire_sa.LoanSchedulers', N'SubInstallmentSequence') IS NULL
BEGIN
    ALTER TABLE [dinspire_sa].[LoanSchedulers] ADD [SubInstallmentSequence] INT NOT NULL
        CONSTRAINT [DF_LoanSchedulers_SubInstallmentSequence] DEFAULT (0);
END
ELSE IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dinspire_sa.LoanSchedulers')
      AND c.name = N'SubInstallmentSequence')
BEGIN
    ALTER TABLE [dinspire_sa].[LoanSchedulers]
        ADD CONSTRAINT [DF_LoanSchedulers_SubInstallmentSequence] DEFAULT (0) FOR [SubInstallmentSequence];
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_LoanSchedulers_LoanSchedulers_ParentLoanSchedulerId'
      AND parent_object_id = OBJECT_ID(N'dinspire_sa.LoanSchedulers'))
BEGIN
    ALTER TABLE [dinspire_sa].[LoanSchedulers] WITH CHECK
    ADD CONSTRAINT [FK_LoanSchedulers_LoanSchedulers_ParentLoanSchedulerId]
        FOREIGN KEY ([ParentLoanSchedulerId]) REFERENCES [dinspire_sa].[LoanSchedulers] ([LoanSchedulerId]);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_LoanSchedulers_ParentLoanSchedulerId'
      AND object_id = OBJECT_ID(N'dinspire_sa.LoanSchedulers'))
BEGIN
    CREATE INDEX [IX_LoanSchedulers_ParentLoanSchedulerId]
        ON [dinspire_sa].[LoanSchedulers] ([ParentLoanSchedulerId]);
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_LoanSchedulers_Parent_Seq'
      AND object_id = OBJECT_ID(N'dinspire_sa.LoanSchedulers'))
BEGIN
    CREATE UNIQUE INDEX [UX_LoanSchedulers_Parent_Seq]
        ON [dinspire_sa].[LoanSchedulers] ([ParentLoanSchedulerId], [SubInstallmentSequence])
        WHERE [ParentLoanSchedulerId] IS NOT NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_LoanSchedulers_Loan_Installment_Base'
      AND object_id = OBJECT_ID(N'dinspire_sa.LoanSchedulers'))
BEGIN
    CREATE UNIQUE INDEX [UX_LoanSchedulers_Loan_Installment_Base]
        ON [dinspire_sa].[LoanSchedulers] ([LoanId], [InstallmentNo])
        WHERE [ParentLoanSchedulerId] IS NULL AND [SubInstallmentSequence] = 0;
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'dinspire_sa.RecoveryPostingIdempotency', N'U') IS NULL
BEGIN
    CREATE TABLE [dinspire_sa].[RecoveryPostingIdempotency] (
        [ClientRequestId] UNIQUEIDENTIFIER NOT NULL,
        [OrgId] INT NOT NULL,
        [BranchId] INT NOT NULL,
        [UserId] INT NOT NULL,
        [RequestHash] NVARCHAR(64) NOT NULL,
        [ResponseJson] NVARCHAR(4000) NULL,
        [CreatedDate] DATETIME2 NOT NULL,
        CONSTRAINT [PK_RecoveryPostingIdempotency] PRIMARY KEY ([ClientRequestId])
    );
    CREATE INDEX [IX_RecoveryPostingIdempotency_Org_Branch_Created]
        ON [dinspire_sa].[RecoveryPostingIdempotency] ([OrgId], [BranchId], [CreatedDate]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'dinspire_sa.RecoveryPostingIdempotency', N'U') IS NOT NULL
    DROP TABLE [dinspire_sa].[RecoveryPostingIdempotency];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_LoanSchedulers_Loan_Installment_Base' AND object_id = OBJECT_ID(N'dinspire_sa.LoanSchedulers'))
    DROP INDEX [UX_LoanSchedulers_Loan_Installment_Base] ON [dinspire_sa].[LoanSchedulers];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_LoanSchedulers_Parent_Seq' AND object_id = OBJECT_ID(N'dinspire_sa.LoanSchedulers'))
    DROP INDEX [UX_LoanSchedulers_Parent_Seq] ON [dinspire_sa].[LoanSchedulers];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LoanSchedulers_ParentLoanSchedulerId' AND object_id = OBJECT_ID(N'dinspire_sa.LoanSchedulers'))
    DROP INDEX [IX_LoanSchedulers_ParentLoanSchedulerId] ON [dinspire_sa].[LoanSchedulers];

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_LoanSchedulers_LoanSchedulers_ParentLoanSchedulerId')
    ALTER TABLE [dinspire_sa].[LoanSchedulers] DROP CONSTRAINT [FK_LoanSchedulers_LoanSchedulers_ParentLoanSchedulerId];
");
        }
    }
}
