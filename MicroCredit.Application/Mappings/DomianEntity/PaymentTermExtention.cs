using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Master;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCredit.Application.Mappings.DomianEntity
{
    public static class PaymentTermExtention
    {
        public static PaymentTermResponse ToPaymentTermResponse(this PaymentTerm paymentTerm)
        {
            return new PaymentTermResponse
            {
                Id = paymentTerm.PaymentTermId,
                PaymentTerm = paymentTerm.PaymentTermName,
                PaymentType = paymentTerm.PaymentType,
                NoOfTerms = paymentTerm.NoOfTerms,
                ProcessingFee = paymentTerm.InsuranceFee?.ToString() is string s ? decimal.Parse(s) : 0m,
                RateOfInterest = paymentTerm.RateOfInterest?.ToString() is string y ? decimal.Parse(y):0m,    
                InsuranceFee = paymentTerm.InsuranceFee?.ToString() is string x ? decimal.Parse(x) : 0m
            };
        }

        public static IEnumerable<PaymentTermResponse> ToPaymentTermResponses(this IEnumerable<PaymentTerm> paymentTermList)
        {
            return paymentTermList.Select(u => u.ToPaymentTermResponse());
        }
    }
}
