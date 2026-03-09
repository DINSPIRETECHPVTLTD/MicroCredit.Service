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
    [Route("MasterLookups")]
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


    }
}
