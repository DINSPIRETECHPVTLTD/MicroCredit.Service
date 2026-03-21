using MicroCredit.Domain.Model.LoanScheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Domain.Interfaces.Service
{
    public interface ILoanSchedulerService
    {
        Task GenerateEMIScheduleAsync(int loanId, int userId, CancellationToken cancellationToken = default);
        Task<IEnumerable<LoanSchedulerResponce>> GetLoanSchedulersByIdAsync(int loanId, CancellationToken cancellationToken = default);
    }
}
