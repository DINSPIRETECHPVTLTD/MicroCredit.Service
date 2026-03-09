using MicroCredit.Api.Helpers;
using MicroCredit.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize]
public class CenterController : ControllerBase
{
    private readonly ICenterService _centerService;
    private readonly ILogger<CenterController> _logger;

    public CenterController(ICenterService centerService, ILogger<CenterController> logger)
    {
        _centerService = centerService;
        _logger = logger;
    }

    [HttpGet("getCenters")]
    public async Task<IActionResult> GetCentersByBranch(CancellationToken cancellationToken)
    {
        var ids = UserClaimsHelper.GetUserIdOrgIdAndBranchId(User);
        if (ids == null) return Unauthorized();

        var (_, _, branchId) = ids.Value;
        if (branchId == null)
            return BadRequest("Branch context is required. Navigate to a branch first.");

        var centers = await _centerService.GetCentersByBranchAsync(branchId.Value, cancellationToken);
        return Ok(centers);
    }
}
