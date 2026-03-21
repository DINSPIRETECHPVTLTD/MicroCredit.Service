using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Infrastructure.Persistence;

namespace MicroCredit.Infrastructure.Repositories;

public class MemberMembershipFeeRepository : IMemberMembershipFeeRepository
{
    private readonly MicroCreditDbContext _context;

    public MemberMembershipFeeRepository(MicroCreditDbContext context)
    {
        _context = context;
    }

    public Task CreateAsync(MemberMembershipFee fee, CancellationToken cancellationToken = default)
    {
        _context.MemberMembershipFees.Add(fee);
        return Task.CompletedTask;
    }
}

