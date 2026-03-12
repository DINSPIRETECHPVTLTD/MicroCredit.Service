using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.PaymentTerm;

namespace MicroCredit.Application.Mappings.DomianEntity;

public static class PaymentTermExtension
{
    public static PaymentTermResponse ToPaymentTermResponse(this PaymentTerm paymentTerm)
    {
        return new PaymentTermResponse
        {
            PaymentTermId = paymentTerm.PaymentTermId,
            PaymentTermName = paymentTerm.PaymentTermName,
            PaymentType = paymentTerm.PaymentType,
            NoOfTerms = paymentTerm.NoOfTerms,
            ProcessingFee = paymentTerm.ProcessingFee,
            RateOfInterest = paymentTerm.RateOfInterest,
            InsuranceFee = paymentTerm.InsuranceFee,
            CreatedAt = paymentTerm.CreatedAt,
            ModifiedAt = paymentTerm.ModifiedAt
        };
    }

    public static IEnumerable<PaymentTermResponse> ToPaymentTermResponses(this IEnumerable<PaymentTerm> paymentTerms)
    {
        return paymentTerms.Select(p => p.ToPaymentTermResponse());
    }

    public static PaymentTerm ToPaymentTermEntity(this CreatePaymentTermRequest request, int createdBy)
    {
        return new PaymentTerm
        {
            PaymentTermName = request.PaymentTermName,
            PaymentType = request.PaymentType,
            NoOfTerms = request.NoOfTerms,
            ProcessingFee = request.ProcessingFee,
            RateOfInterest = request.RateOfInterest,
            InsuranceFee = request.InsuranceFee,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public static void UpdateFromRequest(this PaymentTerm paymentTerm, UpdatePaymentTermRequest request, int modifiedBy)
    {
        paymentTerm.PaymentTermName = request.PaymentTermName;
        paymentTerm.PaymentType = request.PaymentType;
        paymentTerm.NoOfTerms = request.NoOfTerms;
        paymentTerm.ProcessingFee = request.ProcessingFee;
        paymentTerm.RateOfInterest = request.RateOfInterest;
        paymentTerm.InsuranceFee = request.InsuranceFee;
        paymentTerm.ModifiedBy = modifiedBy;
        paymentTerm.ModifiedAt = DateTime.UtcNow;
    }
}