using MicroCredit.Domain.Model.Member;

namespace MicroCredit.Domain.Interfaces.Services;

public interface IMembersService
{
    Task<IEnumerable<MemberResponse>> GetMembersAsync(int branchId, CancellationToken cancellationToken = default);
    Task<MemberResponse?> GetMemberAsync(int id, CancellationToken cancellationToken = default);
    Task<MemberResponse> CreateMemberAsync(MemberRequest request, CancellationToken cancellationToken = default);
    Task<MemberResponse> UpdateMemberAsync(int id, MemberRequest request, CancellationToken cancellationToken = default);
}
