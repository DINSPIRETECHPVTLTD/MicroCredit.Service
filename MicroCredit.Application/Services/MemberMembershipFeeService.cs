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
    private readonly ILedgerRecordService _ledgerRecordService;

    public MemberMembershipFeeService(IUnitOfWork unitOfWork, ILedgerRecordService ledgerRecordService)
    {
        _unitOfWork = unitOfWork;
        _ledgerRecordService = ledgerRecordService;
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
        await _unitOfWork.InsuranceClaimFinancialSummaries.AccumulateJoiningFeeAsync(
            fee.Amount,
            cancellationToken);
        await _unitOfWork.CompleteAsync();

        if (fee.Amount > 0m)
        {
            var paidToUserId = request.CollectedBy is > 0 ? request.CollectedBy.Value : context.UserId;
            var paymentDate = request.PaidDate ?? DateTime.UtcNow;

            await _ledgerRecordService.RecordDepositAsync(
                paidToUserId: paidToUserId,
                amount: fee.Amount,
                paymentDate: paymentDate,
                createdBy: context.UserId,
                createdDate: DateTime.UtcNow,
                transactionType: "Member Joining Fee",
                referenceId: fee.Id,
                comments: string.IsNullOrWhiteSpace(request.Comments)
                    ? $"Member joining fee for Member ID: {fee.MemberId} (fee record {fee.Id})."
                    : request.Comments,
                cancellationToken: cancellationToken);
        }

        return fee.ToResponse();
    }
}

