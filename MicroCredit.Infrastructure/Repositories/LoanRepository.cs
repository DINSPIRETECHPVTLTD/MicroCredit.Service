using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
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

    public async Task<IEnumerable<Loan>> GetLoanByMemId(int memberId, CancellationToken cancellationToken = default)
    {
        return await _context.Loans.Where(l => l.MemberId == memberId).ToListAsync(cancellationToken);
    }

    public async Task AddLoanAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        await _context.Loans.AddAsync(loan, cancellationToken);
    }

    public async Task<IEnumerable<Loan>> GetActiveLoanAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var member=_context.Members.Where(m => m.Id == memberId).FirstOrDefaultAsync(cancellationToken);
        return await _context.Loans.Where(v => v.MemberId == memberId && v.IsDeleted == true).ToListAsync(cancellationToken);
       
    }
}