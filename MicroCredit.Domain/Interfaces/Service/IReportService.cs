using MicroCredit.Domain.Model.Report;

namespace MicroCredit.Domain.Interfaces.Service;

public interface IReportService
{
    Task<List<ReportPocCenterResponseDto>> GetPocsByBranchIdAsync(int branchId);
    Task<List<ReportMembersByPocResponseDto>> GetMembersByPocIdAsync(int branchId, int pocId);
    Task<List<ReportMembersByPocResponseDto>> GetMembersByPocIdsAsync(int branchId, IReadOnlyList<int> pocIds);
    Task<ReportSummaryResponseDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<byte[]> GetMemberWiseCollectionSheet(int orgId, int? branchId);
}
