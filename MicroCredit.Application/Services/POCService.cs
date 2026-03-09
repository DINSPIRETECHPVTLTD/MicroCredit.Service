

using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.Poc;
using MicroCredit.Infrastructure.Repositories;

namespace MicroCredit.Application.Services;

public class POCService: IPOCService
{
    private readonly IUnitOfWork unitOfWork;
    public POCService(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }  

    public async Task<PocResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var poc = await unitOfWork.POCs.GetByIdAsync(id, cancellationToken);
            if (poc == null)
                throw new Exception("POC not found");
            return poc.ToPocResponse();
         }
    public async Task<IEnumerable<PocResponse>> GetPOCsByBranchIdAsync(int branchId, CancellationToken cancellationToken = default)
    {
        var pocs = await unitOfWork.POCs.GetByBranchIdAsync(branchId, cancellationToken);

        return pocs.Select(p => new PocResponse
        {
            Id = p.Id,
            FirstName = p.FirstName,
            LastName = p.LastName,
            PhoneNumber = p.PhoneNumber,
            CenterId = p.CenterId,
            // Add other properties as needed
        });
    }
}
