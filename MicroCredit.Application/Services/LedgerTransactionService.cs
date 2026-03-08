using MicroCredit.Application.Mappings.DomianEntity;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.Fund;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Application.Services
{
    public class LedgerTransactionService : ILedgerTransactionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public LedgerTransactionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<ExpenseResponse>> GetExpensesAsync(int orgId, CancellationToken cancellationToken = default)
        {
            return (await _unitOfWork
                .LedgerTransaction
                .GetExpensesAsync(orgId, cancellationToken))
                .ToExpenseResponses();
        }
    }
}
