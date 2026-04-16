using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Member;

namespace MicroCredit.Domain.Interfaces.Repository;

public interface IMemberRepository
{
    Task<Member?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Member>> GetMembersByBranchAsync(int branchId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByAadhaarAsync(string aadhaar, int? excludeMemberId = null, CancellationToken cancellationToken = default);
    Task CreateAsync(Member member, CancellationToken cancellationToken = default);
    Task UpdateAsync(Member member, CancellationToken cancellationToken = default);
    Task<IEnumerable<Member>> SearchMembersByBranchAsync(SearchMemberRequest request, CancellationToken cancellationToken = default);
}
