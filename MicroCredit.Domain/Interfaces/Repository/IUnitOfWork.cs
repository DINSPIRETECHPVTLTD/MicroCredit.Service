namespace MicroCredit.Domain.Interfaces.Repository;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IBranchRepository Branches { get; }
    ILoanRepository Loans { get; }
    IInvestmentRepository Investments { get; }
    ILedgerBalanceRepository LedgerBalances { get; }
    ILedgerTransactionRepository LedgerTransaction { get; }
    IMasterLookupRepository MasterLookups { get; }
    IPaymentTermRepository PaymentTerms { get; }
    IPOCRepository POCs { get; }
    ICenterRepository Centers { get; }
    IMemberRepository Members { get; }
    IMemberMembershipFeeRepository MemberMembershipFees { get; }
    ILoanSchedulersRepository LoanSchedulers { get; }


    Task<int> CompleteAsync();
}
