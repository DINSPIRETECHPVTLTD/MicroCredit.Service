using MicroCredit.Domain.Common;
using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.Report;
using MicroCredit.Api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IUserContext _userContext;

    public ReportController(IReportService reportService, IUserContext userContext)
    {
        _reportService = reportService;
        _userContext = userContext;
    }

    [HttpGet("pocs-by-branch/{branchId:int}")]
    public async Task<IActionResult> GetPocsByBranchId(int branchId)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();
        var role = UserClaimsHelper.GetUserRole(User);
        if (role == UserRole.BranchAdmin || role == UserRole.Staff)
        {
            if (!_userContext.BranchId.HasValue)
                return StatusCode(StatusCodes.Status403Forbidden, "Branch context is required.");
            if (_userContext.BranchId.Value != branchId)
                return StatusCode(StatusCodes.Status403Forbidden, "You can access only your branch data.");
        }

        if (branchId <= 0)
            return BadRequest("branchId must be greater than 0.");

        var data = await _reportService.GetPocsByBranchIdAsync(branchId);
        if (data == null || !data.Any())
            return Ok(new
            {
                message = "No POC found in current branch",
                data = new List<ReportPocCenterResponseDto>()
            });

        return Ok(data);
    }

    [HttpGet("members-by-poc/{branchId:int}/{pocId:int}")]
    public async Task<IActionResult> GetMembersByPocId(int branchId, int pocId)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();
        var role = UserClaimsHelper.GetUserRole(User);
        if (role == UserRole.BranchAdmin || role == UserRole.Staff)
        {
            if (!_userContext.BranchId.HasValue)
                return StatusCode(StatusCodes.Status403Forbidden, "Branch context is required.");
            if (_userContext.BranchId.Value != branchId)
                return StatusCode(StatusCodes.Status403Forbidden, "You can access only your branch data.");
        }

        if (branchId <= 0 || pocId <= 0)
            return BadRequest("branchId and pocId must be greater than 0.");

        var data = await _reportService.GetMembersByPocIdAsync(branchId, pocId);
        return Ok(data ?? new List<ReportMembersByPocResponseDto>());
    }

    /// <summary>
    /// Bulk endpoint: returns members due today/tomorrow for multiple POCs in a single request.
    /// </summary>
    [HttpPost("members-by-pocs/{branchId:int}")]
    public async Task<IActionResult> GetMembersByPocIds(
        int branchId,
        [FromBody] int[] pocIds,
        CancellationToken cancellationToken)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();
        var role = UserClaimsHelper.GetUserRole(User);
        if (role == UserRole.BranchAdmin || role == UserRole.Staff)
        {
            if (!_userContext.BranchId.HasValue)
                return StatusCode(StatusCodes.Status403Forbidden, "Branch context is required.");
            if (_userContext.BranchId.Value != branchId)
                return StatusCode(StatusCodes.Status403Forbidden, "You can access only your branch data.");
        }

        if (branchId <= 0)
            return BadRequest("branchId must be greater than 0.");

        if (pocIds == null || pocIds.Length == 0)
            return BadRequest("pocIds must be provided.");

        var distinctPocIds = pocIds.Where(id => id > 0).Distinct().ToArray();
        if (distinctPocIds.Length == 0)
            return BadRequest("pocIds must contain valid positive ids.");

        var data = await _reportService.GetMembersByPocIdsAsync(branchId, distinctPocIds);
        return Ok(data ?? new List<ReportMembersByPocResponseDto>());
    }
    [HttpGet("MemberWiseCollectionSheet")]
    public async Task<IActionResult> GetMemberWiseCollectionsheet()
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0 || _userContext.BranchId == 0)
            return Unauthorized();

        var fileBytes = await _reportService.GetMemberWiseCollectionSheet(_userContext.OrgId, _userContext.BranchId);

        if (fileBytes == null || fileBytes.Length == 0)
            return NotFound("No data found for the given organisation.");

        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"MemberWiseCollection_{DateTime.Today:yyyyMMdd}.xlsx"
        );
    }
}
