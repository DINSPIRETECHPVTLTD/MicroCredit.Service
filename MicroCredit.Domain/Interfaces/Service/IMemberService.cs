using MicroCredit.Domain.Common;
using MicroCredit.Domain.Model.Member;

namespace MicroCredit.Domain.Interfaces.Service;

public interface IMemberService
{
    Task<MemberResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<MemberResponse>> GetMembersByBranchAsync(int branchId, CancellationToken cancellationToken = default);
    Task<MemberResponse> CreateAsync(CreateMemberRequest request, IUserContext userContext, CancellationToken cancellationToken = default);
    Task<MemberResponse> UpdateAsync(int id, UpdateMemberRequest request, IUserContext userContext, CancellationToken cancellationToken = default);
    Task<bool> MarkAsInactiveAsync(int id, int modifiedBy, CancellationToken cancellationToken = default);

    Task<IEnumerable<MemberResponse>> SearchMemebersByBranchAsync(SearchMemberRequest request, CancellationToken cancellationToken = default);
}
