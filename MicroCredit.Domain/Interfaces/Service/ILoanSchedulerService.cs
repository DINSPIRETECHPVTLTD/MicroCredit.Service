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
    }
}
