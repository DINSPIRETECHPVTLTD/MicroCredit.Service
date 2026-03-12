
using MicroCredit.Domain.Entities;

namespace MicroCredit.Domain.Interfaces.Repository;

public interface IPOCRepository
{
    Task<POC?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<POC>> GetByBranchIdAsync(int branchId, CancellationToken cancellationToken = default);
}
