namespace MicroCredit.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IBranchRepository Branches { get; }

    Task<int> CompleteAsync();
}
