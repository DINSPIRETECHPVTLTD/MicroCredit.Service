using MicroCredit.Domain.Entities;
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

    public async Task<StaffSchedulesReportResponseDto> GetStaffSchedulesReportByBranchAsync(
        int branchId,
        CancellationToken cancellationToken = default)
    {
        // DbContext is scoped per request — queries must not run concurrently on the same instance.
        var staffList = await _unitOfWork.Reports.GetPocCollectionStaffByBranchAsync(branchId, cancellationToken);
        var pocList = await _unitOfWork.Reports.GetStaffReportPocsByBranchAsync(branchId, cancellationToken);
        var memberList = await _unitOfWork.Reports.GetStaffReportMembersByBranchAsync(branchId, cancellationToken);

        var membersByPoc = memberList
            .GroupBy(m => m.PocId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var pocsByStaff = pocList
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var staffById = staffList.ToDictionary(s => s.UserId);
        foreach (var poc in pocList)
        {
            if (staffById.ContainsKey(poc.UserId))
                continue;

            staffById[poc.UserId] = new PocCollectionStaffReportDto
            {
                UserId = poc.UserId,
                UserFullName = poc.UserFullName,
                UserRole = poc.UserRole,
            };
        }

        var staffNodes = staffById.Values
            .OrderBy(s => s.UserFullName, StringComparer.OrdinalIgnoreCase)
            .Select(staff =>
        {
            var staffPocs = pocsByStaff.GetValueOrDefault(staff.UserId) ?? new List<StaffReportPocRowDto>();
            return new StaffSchedulesStaffNodeDto
            {
                UserId = staff.UserId,
                UserFullName = staff.UserFullName,
                UserRole = staff.UserRole,
                Pocs = staffPocs.Select(poc => new StaffSchedulesPocNodeDto
                {
                    PocId = poc.PocId,
                    PocFullName = poc.PocFullName,
                    CenterId = poc.CenterId,
                    Members = membersByPoc.GetValueOrDefault(poc.PocId) ?? new List<StaffReportMemberRowDto>(),
                }).ToList(),
            };
        }).ToList();

        return new StaffSchedulesReportResponseDto { Staff = staffNodes };
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
