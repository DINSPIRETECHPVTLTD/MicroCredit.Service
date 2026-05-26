using MicroCredit.Domain.Common;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Interfaces.Services;
using MicroCredit.Domain.Model.Center;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class CenterController : ControllerBase
    {
        private readonly ICenterService _centerService;
        private readonly IUserContext _userContext;

        public CenterController(ICenterService centerService, IUserContext userContext)
        {
            _centerService = centerService;
            _userContext = userContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetCenters()
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();
            if (!_userContext.BranchId.HasValue)
                return BadRequest("BranchId is required to get centers.");

            var centers = await _centerService.GetCentersAsync(_userContext.BranchId.Value);
            var list = centers.ToList();
            if (list.Count == 0)
            {
                return Ok(new
                {
                    message = "No centers found",
                    data = list
                });
            }

            return Ok(list);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCenterRequest request, CancellationToken cancellationToken)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();
            var center = await _centerService.CreateCenterAsync(request, _userContext, cancellationToken);
            return Ok(center);
        }
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCenterRequest request, CancellationToken cancellationToken)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();
            var center = await _centerService.UpdateCenterAsync(id, request, _userContext, cancellationToken);
            return Ok(center);
        }

        [HttpDelete("{id:int}/inactive")]
        public async Task<IActionResult> MarkAsInactive(int id, CancellationToken cancellationToken)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();
            var result = await _centerService.MarkAsInactive(id, _userContext.UserId, cancellationToken);

            return Ok(result);
        }

    }
}
