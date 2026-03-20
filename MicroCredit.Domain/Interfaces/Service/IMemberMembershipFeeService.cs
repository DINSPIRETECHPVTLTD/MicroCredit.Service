using MicroCredit.Domain.Common;
using MicroCredit.Domain.Model.MemberMembershipFee;

namespace MicroCredit.Domain.Interfaces.Service;

public interface IMemberMembershipFeeService
{
    Task<MemberMembershipFeeResponse> CreateAsync(CreateMemberMembershipFeeRequest request, IUserContext context, CancellationToken cancellationToken = default);
}

