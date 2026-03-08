namespace MicroCredit.Domain.Interfaces.Repository;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IBranchRepository Branches { get; }
    ILoanRepository Loans { get; }
    IInvestmentRepository Investments { get; }
    ILedgerBalanceRepository LedgerBalances { get; }
    ILedgerTransactionRepository LedgerTransaction { get; }

    Task<int> CompleteAsync();
}
