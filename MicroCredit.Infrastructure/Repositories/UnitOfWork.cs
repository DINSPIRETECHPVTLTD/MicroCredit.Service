using MicroCredit.Domain.Interfaces;
using MicroCredit.Infrastructure.Persistence;

namespace MicroCredit.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly MicroCreditDbContext _context;

    public IUserRepository Users { get; }

    public UnitOfWork(MicroCreditDbContext context)
    {
        _context = context;
        Users = new UserRepository(_context);
    }

    public async Task<int> CompleteAsync()
        => await _context.SaveChangesAsync();

    public void Dispose()
        => _context.Dispose();
}
