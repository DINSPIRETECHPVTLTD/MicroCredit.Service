namespace MicroCredit.Domain.Interfaces.Repository;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IBranchRepository Branches { get; }
    ILoanRepository Loans { get; }
    IMasterLookupRepository MasterLookups { get; }
    IPOCRepository POCs { get; }


    Task<int> CompleteAsync();
}
