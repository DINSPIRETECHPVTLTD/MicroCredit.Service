using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Fund;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Application.Mappings.DomianEntity
{
    public static class LedgerBalancesExtension
    {
        public static LedgerBalanceResponse ToLedgerBalanceResponse(this Ledger ledger)
        {
            return new LedgerBalanceResponse
            {
                Id = ledger.Id,
                UserId = ledger.UserId,
                Amount = ledger.Amount,
                InsuranceAmount = ledger.InsuranceAmount ?? 0m
            };

        }
        public static IEnumerable<LedgerBalanceResponse> ToLedgerBalanceResponses(this IEnumerable<Ledger> ledgerBalancesList)
        {
            return ledgerBalancesList.Select(l => l.ToLedgerBalanceResponse());
        }


    }
}
