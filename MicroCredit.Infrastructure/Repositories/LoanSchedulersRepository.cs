using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MicroCredit.Infrastructure.Repositories;
public class LoanSchedulersRepository : ILoanSchedulersRepository
{
    private readonly MicroCreditDbContext _context;
    public LoanSchedulersRepository(MicroCreditDbContext context)
    {
        _context = context;
    }

    public async Task<LoanScheduler?> GetByLoanId(int Loanid, CancellationToken cancellationToken)
    {
        var scheduler = await _context.LoanSchedulers
        .FirstOrDefaultAsync(ls => ls.LoanId == Loanid, cancellationToken);

        if (scheduler == null)
        {
            throw new Exception($"LoanSchedule with ID {Loanid} not found.");
        }

        return scheduler;
    }


}
