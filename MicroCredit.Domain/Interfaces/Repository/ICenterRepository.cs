using MicroCredit.Domain.Entities;

namespace MicroCredit.Domain.Interfaces.Repository;

public interface ICenterRepository
{
    Task<IEnumerable<Center>> GetCentersByBranchAsync(int branchId, CancellationToken cancellationToken = default);
    Task CreateAsync(Center center, CancellationToken cancellationToken = default);
}
