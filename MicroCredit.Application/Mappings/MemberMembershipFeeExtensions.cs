using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.MemberMembershipFee;

namespace MicroCredit.Application.Mappings;

public static class MemberMembershipFeeExtensions
{
    public static MemberMembershipFeeResponse ToResponse(this MemberMembershipFee fee)
    {
        return new MemberMembershipFeeResponse
        {
            Id = fee.Id,
            MemberId = fee.MemberId,
            Amount = fee.Amount,
            PaidDate = fee.PaidDate,
            CollectedBy = fee.CollectedBy,
            PaymentMode = fee.PaymentMode,
            Comments = fee.Comments,
            CreatedBy = fee.CreatedBy,
            CreatedAt = fee.CreatedAt,
        };
    }
}

