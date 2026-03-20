using MicroCredit.Application.Mappings;
using MicroCredit.Domain.Common;
using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.MemberMembershipFee;

namespace MicroCredit.Application.Services;

public class MemberMembershipFeeService : IMemberMembershipFeeService
{
    private readonly IUnitOfWork _unitOfWork;

    public MemberMembershipFeeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<MemberMembershipFeeResponse> CreateAsync(CreateMemberMembershipFeeRequest request, IUserContext context, CancellationToken cancellationToken = default)
    {
        if (context.UserId == 0)
            throw new UnauthorizedAccessException("User context is required.");

        var member = await _unitOfWork.Members.GetByIdAsync(request.MemberId, cancellationToken);
        if (member == null)
            throw new Exception("Member not found.");

        var fee = new MemberMembershipFee(
            memberId: request.MemberId,
            amount: request.Amount,
            createdBy: context.UserId,
            collectedBy: request.CollectedBy,
            paidDate: request.PaidDate,
            paymentMode: request.PaymentMode,
            comments: request.Comments
        );

        await _unitOfWork.MemberMembershipFees.CreateAsync(fee, cancellationToken);
        await _unitOfWork.CompleteAsync();

        return fee.ToResponse();
    }
}

