using MicroCredit.Domain.Interfaces.Repository;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.Report;
using Microsoft.Extensions.Logging;

namespace MicroCredit.Application.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReportService> _logger;

    public ReportService(IUnitOfWork unitOfWork, ILogger<ReportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
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

    public async Task<ReportSummaryResponseDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching report summary data.");
        return await _unitOfWork.Reports.GetSummaryAsync(cancellationToken);
    }

    public async Task<byte[]> GetMemberWiseCollectionSheet(int orgId, int? branchId)
    {
        return await _unitOfWork.Reports.GetMemberWiseCollectionSheet(orgId, branchId);
    }
}
