-- Deployed on dinspire_mcs_dev; matches prior EF migration (20260320120000_AddBranchLoansReportStoredProcedure).
-- Adjust schema qualifiers if your objects live under dbo instead of dinspire_sa.

CREATE OR ALTER PROCEDURE sp_BranchLoansReport
    @BranchId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ls.LoanId,
        l.MemberId,
        LTRIM(RTRIM(mb.FirstName + ' ' + ISNULL(mb.MiddleName + ' ', '') + mb.LastName)) AS FullName,
        l.TotalAmount AS LoanTotalAmount,
        CAST(COUNT(CASE WHEN ls.SubInstallmentSequence = 0 THEN 1 END) AS VARCHAR) + '/' +
        CAST(SUM(CASE WHEN ls.SubInstallmentSequence = 0 AND ls.Status IN ('Paid', 'Partial') THEN 1 ELSE 0 END) AS VARCHAR) AS NoOfTerms,
        SUM(CASE WHEN ls.Status IN ('Paid', 'Partial') THEN ls.PaymentAmount ELSE 0 END) AS TotalAmountPaid,
        SUM(ls.ActualEmiAmount) AS SchedulerTotalAmount,
        SUM(CASE WHEN ls.Status = 'Not Paid' THEN ls.ActualEmiAmount ELSE 0 END) AS RemainingBal
    FROM [dinspire_sa].[LoanSchedulers] ls
    LEFT JOIN [dinspire_sa].[Loans] l ON ls.LoanId = l.Id AND l.Status = 'Active'
    LEFT JOIN [dinspire_sa].[Members] mb ON l.MemberId = mb.Id
    WHERE l.MemberId IN (
        SELECT Id
        FROM [dinspire_sa].[Members]
        WHERE CenterId IN (
            SELECT Id
            FROM [dinspire_sa].[Centers]
            WHERE BranchId = @BranchId
        )
    )
    GROUP BY ls.LoanId, l.MemberId, mb.FirstName, mb.MiddleName, mb.LastName, l.TotalAmount
    ORDER BY ls.LoanId;
END;
