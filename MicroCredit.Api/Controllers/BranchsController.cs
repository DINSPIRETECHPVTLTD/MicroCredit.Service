using MicroCredit.Api.Helpers;
using MicroCredit.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class BranchsController : ControllerBase
    {
        private readonly IBranchsService _branchService;
        private readonly ILogger<BranchsController> _logger;

        public BranchsController(IBranchsService branchService, ILogger<BranchsController> logger)
        {
            _branchService = branchService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetBranches()
        {
            var ids = UserClaimsHelper.GetUserIdAndOrgId(User);
            if (ids == null) return Unauthorized();
            var (_, orgId) = ids.Value;
            var branches = await _branchService.GetBranchsAsync(orgId);
            return Ok(branches);
        }
    }
}
