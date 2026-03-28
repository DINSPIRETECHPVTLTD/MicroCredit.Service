using MicroCredit.Domain.Model.Report;

namespace MicroCredit.Domain.Interfaces.Repository;

public interface IReportRepository
{
    Task<List<ReportPocCenterResponseDto>> GetPocsByBranchIdAsync(int branchId);
    Task<List<ReportMembersByPocResponseDto>> GetMembersByPocIdAsync(int branchId, int pocId);
}
