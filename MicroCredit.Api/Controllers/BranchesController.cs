using MicroCredit.Api.Helpers;
using MicroCredit.Application.Services;
using MicroCredit.Domain.Common;
using MicroCredit.Domain.Interfaces.Services;
using MicroCredit.Domain.Model.Branch;
using MicroCredit.Domain.Model.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class BranchesController : ControllerBase
    {
        private readonly IBranchsService _branchService;
        private readonly ILogger<BranchesController> _logger;
        private readonly IUserContext _userContext;

        public BranchesController(IBranchsService branchService, ILogger<BranchesController> logger,IUserContext userContext)
        {
            _branchService = branchService;
            _logger = logger;
            _userContext = userContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetBranches()
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();

            var branches = await _branchService.GetBranchsAsync(_userContext.OrgId);
            return Ok(branches);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBranchRequest request, CancellationToken cancellationToken)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();
            var branch = await _branchService.CreateBranchAsync(request, _userContext, cancellationToken);
            return Ok(branch);
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateBranchRequest request, CancellationToken cancellationToken)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();
            var branch = await _branchService.UpdateBranchAsync(id, request, _userContext, cancellationToken);
            return Ok(branch);
        }

        [HttpDelete("{id:int}/inactive")]
        public async Task<IActionResult> MarkAsInactive(int id, CancellationToken cancellationToken)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();
            var result = await _branchService.MarkAsInactive(id, _userContext, cancellationToken);

            return Ok(result);
        }
    }
}
