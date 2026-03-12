using MicroCredit.Domain.Model.Poc;
namespace MicroCredit.Domain.Interfaces.Service;

public interface IPOCService
{
        Task<PocResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
        Task<IEnumerable<PocResponse>> GetPOCsByBranchIdAsync(int branchId, CancellationToken cancellationToken = default);


}
