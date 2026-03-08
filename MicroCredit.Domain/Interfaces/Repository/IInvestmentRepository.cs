using MicroCredit.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Interfaces.Repository
{
    public interface IInvestmentRepository
    {
        Task<IEnumerable<Investment>> GetInvestmentsAsync(int orgId, CancellationToken cancellationToken = default);
    }
}
