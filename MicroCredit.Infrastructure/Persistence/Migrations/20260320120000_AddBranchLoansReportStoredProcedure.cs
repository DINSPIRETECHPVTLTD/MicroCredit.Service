using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MicroCredit.Infrastructure.Persistence;

#nullable disable

namespace MicroCredit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(MicroCreditDbContext))]
    [Migration("20260320120000_AddBranchLoansReportStoredProcedure")]
    public partial class AddBranchLoansReportStoredProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
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
        CAST(COUNT(ls.InstallmentNo) AS VARCHAR) + '/' + 
        CAST(SUM(CASE WHEN ls.Status = 'Paid' THEN 1 ELSE 0 END) AS VARCHAR) AS NoOfTerms,
        SUM(CASE WHEN ls.Status = 'Paid' THEN ls.PaymentAmount ELSE 0 END) AS TotalAmountPaid,
        SUM(ls.ActualEmiAmount) AS SchedulerTotalAmount,
        SUM(ls.ActualEmiAmount) - SUM(CASE WHEN ls.Status = 'Paid' THEN ls.PaymentAmount ELSE 0 END) AS RemainingBal
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
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
IF OBJECT_ID('sp_BranchLoansReport', 'P') IS NOT NULL
    DROP PROCEDURE sp_BranchLoansReport;");
        }
    }
}
