using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Fund;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Application.Mappings.DomianEntity
{
    public static class LedgerTransactionExtension
    {
        public static ExpenseResponse ToExpensesResponse(this LedgerTransaction lt)
        {
            return new ExpenseResponse
            {
                Id = lt.Id,
                PaidFromUserId = lt.PaidFromUserId,
                PaidToUserId = lt.PaidToUserId,
                Amount = lt.Amount,
                PaymentDate = lt.PaymentDate,
                CreatedBy = lt.CreatedBy,
                CreatedDate = lt.CreatedDate,
                TransactionType = lt.TransactionType,
                ReferenceId = lt.ReferenceId,
                Comments = lt.Comments
            };

        }
        public static IEnumerable<ExpenseResponse> ToExpenseResponses(this IEnumerable<LedgerTransaction> ledgerTransactionList)
        {
            return ledgerTransactionList.Select(l => l.ToExpensesResponse());
        }


    }
}
