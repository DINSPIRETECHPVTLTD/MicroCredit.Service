using MicroCredit.Domain.Model.Report;

namespace MicroCredit.Domain.Interfaces.Repository;

public interface IReportRepository
{
    Task<List<ReportPocCenterResponseDto>> GetPocsByBranchIdAsync(int branchId);
    Task<List<ReportMembersByPocResponseDto>> GetMembersByPocIdAsync(int branchId, int pocId);
    Task<List<ReportMembersByPocResponseDto>> GetMembersByPocIdsAsync(int branchId, IReadOnlyList<int> pocIds);
    Task<ReportSummaryResponseDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<byte[]> GetMemberWiseCollectionSheet(int orgId, int? branchId);
    byte[] Generate(List<MemberWiseCollectionResponseDto> data);
}
