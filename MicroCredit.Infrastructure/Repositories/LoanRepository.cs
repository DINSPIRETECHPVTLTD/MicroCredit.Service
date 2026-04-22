using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Model.Loan;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MicroCredit.Infrastructure.Repositories;

public class LoanRepository : ILoanRepository
{
    private readonly MicroCreditDbContext _context;

    public LoanRepository(MicroCreditDbContext context)
    {
        _context = context;
    }

    public async Task<Loan?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Loans
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);

    }

    public async Task<IEnumerable<Loan>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Loans
            .Where(l => !l.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ActiveLoanResponse>> GetLoanByMemId(int memberId, CancellationToken cancellationToken = default)
    {
        var memberIdParam = new Microsoft.Data.SqlClient.SqlParameter("@MemberId", memberId);

        return await _context.Database
            .SqlQueryRaw<ActiveLoanResponse>(
                "EXEC sp_MemberLoanReport @MemberId",
                memberIdParam)
            .ToListAsync(cancellationToken);
    }

    public async Task AddLoanAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        await _context.Loans.AddAsync(loan, cancellationToken);
    }

    public async Task<IEnumerable<ActiveLoanResponse>> GetActiveLoansAsync(int branchId, CancellationToken cancellationToken = default)

    {
        var branchIdParam = new Microsoft.Data.SqlClient.SqlParameter("@BranchId", branchId);

        return await _context.Database
            .SqlQueryRaw<ActiveLoanResponse>(
                "EXEC sp_BranchLoansReport @BranchId",
                branchIdParam)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOpenSchedulersAsync(int loanId, CancellationToken cancellationToken = default)
    {
        return await _context.LoanSchedulers
            .AnyAsync(
                ls => ls.LoanId == loanId && ls.Status != "Paid",
                cancellationToken);
    }

    public async Task<bool> HasOpenLoanForMemberAsync(int memberId, CancellationToken cancellationToken = default)
    {
        return await _context.Loans.AnyAsync(
            loan => loan.MemberId == memberId &&
                    !loan.IsDeleted &&
                    (loan.Status == "Active" || loan.Status == "Defaulted"),
            cancellationToken);
    }
}