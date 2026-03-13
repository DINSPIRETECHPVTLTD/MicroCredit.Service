using MicroCredit.Domain.Model.Fund;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Interfaces.Service
{
    public interface IInvestmentsService
    {
        Task<IEnumerable<InvestmentResponse>> GetInvestmentsAsync(int orgId, CancellationToken cancellationToken = default);
        Task<int> CreateInvestmentAsync(CreateInvestmentRequest request, int createdByUserId, CancellationToken cancellationToken = default);
    }
}
