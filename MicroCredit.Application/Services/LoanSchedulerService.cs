using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.LoanScheduler;
using MicroCredit.Domain.Model.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Application.Services
{
    public class LoanSchedulerService : ILoanSchedulerService
    {
        private readonly IUnitOfWork _unitOfWork;
        public LoanSchedulerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<IEnumerable<LoanSchedulerResponce>> GetLoanSchedulersByIdAsync(int loanId, CancellationToken cancellationToken = default)
        {
            return (await _unitOfWork.LoanSchedulers.GetLoanSchedulersByIdAsync(loanId, cancellationToken)).ToLoanSchedulerResponces();
        }

     
    }
}
