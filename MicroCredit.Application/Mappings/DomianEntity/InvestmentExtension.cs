using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Fund;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Application.Mappings.DomianEntity
{
    public static class InvestmentExtension
    {
        public static InvestmentResponse ToInvestmentResponse(this Investment investment)
        {
            return new InvestmentResponse
            {
                Id = investment.Id,
                UserId = investment.UserId,
                Amount = investment.Amount,
                InvestmentDate = investment.InvestmentDate,
                CreatedById = investment.CreatedById,
                CreatedDate = investment.CreatedDate
            };

        }
        public static IEnumerable<InvestmentResponse> ToInvestmentResponses(this IEnumerable<Investment> investmentList)
        {
            return investmentList.Select(l => l.ToInvestmentResponse());
        }


    }
}
