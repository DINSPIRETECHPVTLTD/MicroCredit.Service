using System.Threading;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Interfaces.Repository;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IBranchRepository Branches { get; }
    IMembersRepository Members { get; }

    Task<int> CompleteAsync(CancellationToken cancellationToken = default);
}
