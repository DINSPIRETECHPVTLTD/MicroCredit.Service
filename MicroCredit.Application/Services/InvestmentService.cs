using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Domain.Model.Fund;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MicroCredit.Domain.Interfaces.Service;

namespace MicroCredit.Application.Services
{
    public class InvestmentService : IInvestmentsService
    {
        private readonly IUnitOfWork _unitOfWork;

        public InvestmentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<InvestmentResponse>> GetInvestmentsAsync(int orgId, CancellationToken cancellationToken = default)
        {
            return (await _unitOfWork
                .Investments
                .GetInvestmentsAsync(orgId, cancellationToken))
                .ToInvestmentResponses();


        }
    }
}
