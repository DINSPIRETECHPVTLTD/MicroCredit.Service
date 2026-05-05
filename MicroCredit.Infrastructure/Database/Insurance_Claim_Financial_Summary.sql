-- Optional: run manually if you do not use `dotnet ef database update`.
-- EF migration AddInsuranceClaimFinancialSummary applies the same shape via Migrations.

IF OBJECT_ID(N'dbo.Insurance_Claim_Financial_Summary', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Insurance_Claim_Financial_Summary
    (
        SummaryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Insurance_Claim_Financial_Summary PRIMARY KEY,

        TotalInsuranceAmount   DECIMAL(18,2) NOT NULL CONSTRAINT DF_Insurance_Claim_Financial_Summary_TotalInsuranceAmount DEFAULT (0),
        TotalClaimedAmount     DECIMAL(18,2) NOT NULL CONSTRAINT DF_Insurance_Claim_Financial_Summary_TotalClaimedAmount DEFAULT (0),
        TotalProcessingFee     DECIMAL(18,2) NOT NULL CONSTRAINT DF_Insurance_Claim_Financial_Summary_TotalProcessingFee DEFAULT (0),
        TotalJoiningFee        DECIMAL(18,2) NOT NULL CONSTRAINT DF_Insurance_Claim_Financial_Summary_TotalJoiningFee DEFAULT (0),
        TotalExpenseAmount     DECIMAL(18,2) NOT NULL CONSTRAINT DF_Insurance_Claim_Financial_Summary_TotalExpenseAmount DEFAULT (0),

        CreatedDate DATETIME NOT NULL CONSTRAINT DF_Insurance_Claim_Financial_Summary_CreatedDate DEFAULT (GETDATE())
    );
END
GO
