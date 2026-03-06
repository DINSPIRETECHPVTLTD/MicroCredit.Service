using MicroCredit.Domain.Interfaces.Repository;

namespace MicroCredit.Domain.Interfaces.Repository;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IBranchRepository Branches { get; }

    ICenterRepository Centers { get; }

    Task<int> CompleteAsync();
}
