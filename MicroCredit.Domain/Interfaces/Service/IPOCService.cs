using MicroCredit.Domain.Common;
using MicroCredit.Domain.Model.Poc;

namespace MicroCredit.Domain.Interfaces.Service;

public interface IPOCService
{
    Task<PocResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PocResponse>> GetPOCsByBranchIdAsync(int branchId, CancellationToken cancellationToken = default);

    Task<PocResponse> CreateAsync(CreatePocRequest request, CancellationToken cancellationToken = default);
    Task<PocResponse> UpdateAsync(int id, UpdatePocRequest request, CancellationToken cancellationToken = default);
    Task<bool> MarkAsInactiveAsync(int id, int modifiedBy, CancellationToken cancellationToken = default);
}