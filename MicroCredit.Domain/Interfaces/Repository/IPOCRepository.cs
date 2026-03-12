using MicroCredit.Domain.Entities;

namespace MicroCredit.Domain.Interfaces.Repository;

public interface IPOCRepository
{
    Task<POC?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<POC>> GetByBranchIdAsync(int branchId, CancellationToken cancellationToken = default);

    Task CreateAsync(POC poc, CancellationToken cancellationToken = default);

    Task UpdateAsync(POC poc, CancellationToken cancellationToken = default);
}