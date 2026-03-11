using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Domain.Common;
using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.Master;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MicroCredit.Application.Services
{
    public  class MasterLookupservice: IMasterLookupservice
    {
        private readonly IUnitOfWork _unitOfWork;

        public MasterLookupservice(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        

        public async Task<IEnumerable<LookupResponse>> GetMasterLookupAsync(string? lookupKey , CancellationToken cancellationToken = default)
        {
            return (await _unitOfWork.MasterLookups.GetMasterLookupAsync(lookupKey, cancellationToken))
                .ToMasterLookupResponses();
        }

        public async Task<int> CreateMasterLookupAsync(CreateLookupRequest request, int userId, CancellationToken cancellationToken = default)
        {
            var entity = MasterLookup.Create(
                request.LookupKey,
                request.LookupCode,
                request.LookupValue,
                request.NumericValue,
                request.SortOrder,
                request.Description,
                userId.ToString());

            await _unitOfWork.MasterLookups.AddAsync(entity, cancellationToken);
            await _unitOfWork.CompleteAsync();

            return entity.Id;
        }

        public async Task<bool> UpdateMasterLookupAsync(int id, UpdateLookupRequest request, int userId, CancellationToken cancellationToken = default)
        {
            var entity = await _unitOfWork.MasterLookups.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                return false;

            entity.Update(
                request.LookupKey,
                request.LookupCode,
                request.LookupValue,
                request.NumericValue,
                request.SortOrder,
                request.Description,
                userId.ToString());

            await _unitOfWork.MasterLookups.UpdateAsync(entity, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return true;
        }

        public async Task<bool> SetInactiveAsync(int id, int userId, CancellationToken cancellationToken = default)
        {
            var entity = await _unitOfWork.MasterLookups.GetByIdAsync(id, cancellationToken);
            if (entity == null)
                return false;

            entity.MarkInactive(userId.ToString());
            await _unitOfWork.MasterLookups.UpdateAsync(entity, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return true;
        }

    }
}
