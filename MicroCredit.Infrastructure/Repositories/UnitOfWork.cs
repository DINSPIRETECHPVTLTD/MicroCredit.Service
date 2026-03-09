using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Infrastructure.Persistence;

namespace MicroCredit.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly MicroCreditDbContext _context;

    public IUserRepository Users { get; }
    public IBranchRepository Branches { get; }
    public ILoanRepository Loans { get; }
    public IMasterLookupRepository MasterLookups { get; }


    public UnitOfWork(MicroCreditDbContext context)
    {
        _context = context;
        Users = new UserRepository(_context);
        Branches = new BranchRepository(_context);
        Loans = new LoanRepository(_context);
        MasterLookups = new MasterLookupRepository(_context);

    }

    public async Task<int> CompleteAsync()
        => await _context.SaveChangesAsync();

    public void Dispose()
        => _context.Dispose();
}
