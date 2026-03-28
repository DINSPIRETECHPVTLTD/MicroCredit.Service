using MicroCredit.Domain.Common;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.Report;
using Microsoft.AspNetCore.Authorization;
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

        if (branchId <= 0)
            return BadRequest("branchId must be greater than 0.");

        var data = await _reportService.GetPocsByBranchIdAsync(branchId);
        if (data == null || !data.Any())
            return NotFound(new { error = "no Poc in current branch" });

        return Ok(data);
    }

    [HttpGet("members-by-poc/{branchId:int}/{pocId:int}")]
    public async Task<IActionResult> GetMembersByPocId(int branchId, int pocId)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();

        if (branchId <= 0 || pocId <= 0)
            return BadRequest("branchId and pocId must be greater than 0.");

        var data = await _reportService.GetMembersByPocIdAsync(branchId, pocId);
        return Ok(data ?? new List<ReportMembersByPocResponseDto>());
    }
}
