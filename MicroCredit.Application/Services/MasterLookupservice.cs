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
        

        public async Task<IEnumerable<LookupResponse>> GetMasterLookupAsync(string lookupKey ,bool isActive , CancellationToken cancellationToken = default)
        {
            return (await _unitOfWork.MasterLookups.GetMasterLookupAsync(lookupKey, isActive, cancellationToken))
                .ToMasterLookupResponses();
        }

    }
}
