using MicroCredit.Api.Helpers;
using MicroCredit.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class BranchController : ControllerBase
    {
        private readonly IBranchService _branchService;
        private readonly ILogger<UserController> _logger;

        public BranchController(IBranchService branchService, ILogger<UserController> logger)
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
