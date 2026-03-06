using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Infrastructure.Persistence;

namespace MicroCredit.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly MicroCreditDbContext _context;

    public IUserRepository Users { get; }
    public IBranchRepository Branches { get; }
    public ICenterRepository Centers { get; }

    public UnitOfWork(MicroCreditDbContext context)
    {
        _context = context;
        Users = new UserRepository(_context);
        Branches = new BranchRepository(_context);
        Centers = new CenterRepository(_context);
    }

    public async Task<int> CompleteAsync()
        => await _context.SaveChangesAsync();

    public void Dispose()
        => _context.Dispose();
}
