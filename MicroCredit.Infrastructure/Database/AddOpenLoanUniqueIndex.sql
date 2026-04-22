/*
Run after cleaning up duplicate open loans.
This index enforces one non-deleted open loan per member.
*/
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Loans_MemberId_OpenLoanUnique'
      AND object_id = OBJECT_ID('dbo.Loans')
)
BEGIN
    CREATE UNIQUE INDEX [IX_Loans_MemberId_OpenLoanUnique]
        ON [dbo].[Loans]([MemberId])
        WHERE [IsDeleted] = 0 AND [Status] IN ('Active', 'Defaulted');
END
