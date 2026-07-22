using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Model.Report;

namespace MicroCredit.Domain.Interfaces.Service;

public interface IReportService
{
    Task<List<ReportPocCenterResponseDto>> GetPocsByBranchIdAsync(int branchId);
    Task<List<ReportMembersByPocResponseDto>> GetMembersByPocIdAsync(int branchId, int pocId, DateTime? scheduleDate = null);
    Task<List<ReportMembersByPocResponseDto>> GetMembersByPocIdsAsync(int branchId, IReadOnlyList<int> pocIds, DateTime? scheduleDate = null);
    Task<StaffSchedulesReportResponseDto> GetStaffSchedulesReportByBranchAsync(int branchId, DateTime? scheduleDate = null, CancellationToken cancellationToken = default);
    Task<ReportSummaryResponseDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<byte[]> GetMemberWiseCollectionSheet(int orgId, int? branchId);
}
