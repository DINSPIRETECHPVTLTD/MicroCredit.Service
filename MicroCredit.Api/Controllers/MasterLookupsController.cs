using MicroCredit.Domain.Common;
using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Interfaces.Services;
using MicroCredit.Domain.Model.Master;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace MicroCredit.Api.Controllers
{
    [Route("[Controller]")]
    [ApiController]
    [Authorize]
    public class MasterLookupsController : ControllerBase
    {
        private readonly IMasterLookupservice _masterlookupsService;
        private readonly IUserContext _userContext;
        private readonly ILogger<MasterLookupsController> _logger;
        public MasterLookupsController(IMasterLookupservice masterlookupsService, IUserContext userContext, ILogger<MasterLookupsController> logger)
        {
            _masterlookupsService = masterlookupsService;
            _userContext = userContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LookupResponse>>> GetMasterLookupAsync(
           [FromQuery] string? lookupKey = null, CancellationToken cancellationToken = default)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();
            return Ok(await _masterlookupsService.GetMasterLookupAsync(lookupKey, cancellationToken));
        }

        [HttpPost]
        public async Task<IActionResult> CreateMasterLookupAsync([FromBody] CreateLookupRequest request, CancellationToken cancellationToken = default)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.LookupKey) ||
                string.IsNullOrWhiteSpace(request.LookupCode) ||
                string.IsNullOrWhiteSpace(request.LookupValue))
            {
                return BadRequest("LookupKey, LookupCode and LookupValue are required.");
            }

            var id = await _masterlookupsService.CreateMasterLookupAsync(request, _userContext.UserId, cancellationToken);
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateMasterLookupAsync(int id, [FromBody] UpdateLookupRequest request, CancellationToken cancellationToken = default)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.LookupKey) ||
                string.IsNullOrWhiteSpace(request.LookupCode) ||
                string.IsNullOrWhiteSpace(request.LookupValue))
            {
                return BadRequest("LookupKey, LookupCode and LookupValue are required.");
            }

            var result = await _masterlookupsService.UpdateMasterLookupAsync(id, request, _userContext.UserId, cancellationToken);
            if (!result)
                return NotFound($"Master lookup with ID {id} not found.");

            return NoContent();
        }

        [HttpDelete("{id:int}/inactive")]
        public async Task<IActionResult> SetInactiveAsync(int id, CancellationToken cancellationToken = default)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();

            var result = await _masterlookupsService.SetInactiveAsync(id, _userContext.UserId, cancellationToken);
            if (!result)
                return NotFound($"Master lookup with ID {id} not found.");

            return NoContent();
        }
        [HttpGet("lookupkey")]
        public IActionResult Getlookupkeys()
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();

            string[] lookupKeys = new string[]
            {
                "PAYMENTMODE",
                "RELATIONSHIP",
                "STATE",

        }
            ;
            return Ok(lookupKeys);
        }
    }
}
