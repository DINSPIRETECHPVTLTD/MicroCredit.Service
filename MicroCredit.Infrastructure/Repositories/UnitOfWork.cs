using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace MicroCredit.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly MicroCreditDbContext _context;

    public IUserRepository Users { get; }
    public IBranchRepository Branches { get; }
    public ILoanRepository Loans { get; }
    public ILoanSchedulersRepository LoanSchedulers { get; }
    public IInvestmentRepository Investments { get; }
    public ILedgerBalanceRepository LedgerBalances { get; }
    public ILedgerTransactionRepository LedgerTransaction { get; }
    public IMasterLookupRepository MasterLookups { get; }
    public IPaymentTermRepository PaymentTerms { get; }

   public IPOCRepository POCs {  get; }
    public ICenterRepository Centers { get; }
    public IMemberRepository Members { get; }
    public IMemberMembershipFeeRepository MemberMembershipFees { get; }
    public IRecoveryPostingRepository RecoveryPostings { get; }

    public UnitOfWork(MicroCreditDbContext context)
    {
        _context = context;
        Users = new UserRepository(_context);
        Branches = new BranchRepository(_context);
        Loans = new LoanRepository(_context);
        LoanSchedulers = new LoanSchedulersRepository(_context);
        Investments = new InvestmentRepository(_context);
        LedgerBalances = new LedgerBalanceRepository(_context);
        LedgerTransaction = new LedgerTransactionRepository(_context);
        MasterLookups = new MasterLookupRepository(_context);
        PaymentTerms = new PaymentTermRepository(_context);
        POCs = new POCRepository(_context);
        Centers = new CenterRepository(_context);
        Members = new MemberRepository(_context);
        MemberMembershipFees = new MemberMembershipFeeRepository(_context);
        RecoveryPostings = new RecoveryPostingRepository(_context);

    }

    public async Task<int> CompleteAsync()
        => await _context.SaveChangesAsync();

    public void Dispose()
        => _context.Dispose();

    public IDbContextTransaction BeginTransactionAsync(CancellationToken cancellationToken = default)
        => _context.Database.BeginTransaction();
}
