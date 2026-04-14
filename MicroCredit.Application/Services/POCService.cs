using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Domain.Common;
using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.Poc;

namespace MicroCredit.Application.Services;

public class POCService : IPOCService
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
        return pocs.ToPocResponses();
    }

    public async Task<PocResponse> CreateAsync(CreatePocRequest request, IUserContext _userContext, CancellationToken cancellationToken = default)
    {
        if (_userContext.UserId == 0)
            throw new UnauthorizedAccessException("User context is required.");
        var entity = new POC(
            firstName: request.FirstName,
            lastName: request.LastName,
            phoneNumber: request.PhoneNumber,
            centerId: request.CenterId,
            createdBy: _userContext.UserId,
            collectionFrequency: request.CollectionFrequency,
            collectionBy: request.CollectionBy,
            middleName: request.MiddleName,
            altPhone: request.AltPhone,
            address1: request.Address1,
            address2: request.Address2,
            city: request.City,
            state: request.State,
            zipCode: request.ZipCode,
            collectionDay: request.CollectionDay
        );
        await unitOfWork.POCs.CreateAsync(entity, cancellationToken);
        await unitOfWork.CompleteAsync();
        return entity.ToPocResponse();
    }

    public async Task<PocResponse> UpdateAsync(int id, UpdatePocRequest request, IUserContext _userContext, CancellationToken cancellationToken = default)
    {
        if (_userContext.UserId == 0)
            throw new UnauthorizedAccessException("User context is required.");
        var poc = await unitOfWork.POCs.GetByIdAsync(id, cancellationToken);
        if (poc == null)
            throw new Exception("POC not found");
        poc.UpdateDetails(
            centerId: request.CenterId,
            firstName: request.FirstName,
            middleName: request.MiddleName,
            lastName: request.LastName,
            phoneNumber: request.PhoneNumber,
            altPhone: request.AltPhone,
            address1: request.Address1,
            address2: request.Address2,
            city: request.City,
            state: request.State,
            zipCode: request.ZipCode,
            collectionDay: request.CollectionDay,
            collectionFrequency: request.CollectionFrequency,
            collectionBy: request.CollectionBy,
            modifiedBy: _userContext.UserId
        );
        await unitOfWork.POCs.UpdateAsync(poc, cancellationToken);
        await unitOfWork.CompleteAsync();
        return poc.ToPocResponse();
    }

    public async Task<bool> MarkAsInactiveAsync(int id, int modifiedBy, CancellationToken cancellationToken = default)
    {
        var poc = await unitOfWork.POCs.GetByIdAsync(id, cancellationToken);
        if (poc == null)
            throw new Exception("POC not found");
        poc.MarkDeleted(modifiedBy);
        await unitOfWork.POCs.UpdateAsync(poc, cancellationToken);
        await unitOfWork.CompleteAsync();
        return true;
    }
}