using MicroCredit.Domain.Entities;

namespace MicroCredit.Domain.Interfaces.Repository;

public interface IMemberMembershipFeeRepository
{
    Task CreateAsync(MemberMembershipFee fee, CancellationToken cancellationToken = default);
}

