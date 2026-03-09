using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Infrastructure.Persistence;

namespace MicroCredit.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly MicroCreditDbContext _context;

    public IUserRepository Users { get; }
    public IBranchRepository Branches { get; }
    public ILoanRepository Loans { get; }
    public IInvestmentRepository Investments { get; }
    public ILedgerBalanceRepository LedgerBalances { get; }
    public ILedgerTransactionRepository LedgerTransaction { get; }
    public IMasterLookupRepository MasterLookups { get; }
    public IPaymentTermRepository PaymentTerms { get; }
    public ICenterRepository Centers { get; }


    public UnitOfWork(MicroCreditDbContext context)
    {
        _context = context;
        Users = new UserRepository(_context);
        Branches = new BranchRepository(_context);
        Loans = new LoanRepository(_context);
        Investments = new InvestmentRepository(_context);
        LedgerBalances = new LedgerBalanceRepository(_context);
        LedgerTransaction = new LedgerTransactionRepository(_context);
        MasterLookups = new MasterLookupRepository(_context);
        PaymentTerms = new PaymentTermRepository(_context);

        Centers = new CenterRepository(_context);
    }

    public async Task<int> CompleteAsync()
        => await _context.SaveChangesAsync();

    public void Dispose()
        => _context.Dispose();
}
