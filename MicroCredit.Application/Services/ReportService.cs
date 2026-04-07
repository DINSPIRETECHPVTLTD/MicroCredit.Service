using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.Report;

namespace MicroCredit.Application.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ReportPocCenterResponseDto>> GetPocsByBranchIdAsync(int branchId)
    {
        return await _unitOfWork.Reports.GetPocsByBranchIdAsync(branchId);
    }

    public async Task<List<ReportMembersByPocResponseDto>> GetMembersByPocIdAsync(int branchId, int pocId)
    {
        return await _unitOfWork.Reports.GetMembersByPocIdAsync(branchId, pocId);
    }

    public async Task<List<ReportMembersByPocResponseDto>> GetMembersByPocIdsAsync(int branchId, IReadOnlyList<int> pocIds)
    {
        return await _unitOfWork.Reports.GetMembersByPocIdsAsync(branchId, pocIds);
    }

    public async Task<byte[]> GetMemberWiseCollectionSheet(int orgId, int? branchId)
    {
        return await _unitOfWork.Reports.GetMemberWiseCollectionSheet(orgId, branchId);
    }
}
