using MicroCredit.Api.Helpers;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers
{
    [Route("investments")]
    [ApiController]
    [Authorize]
    public class InvestmentsController : ControllerBase
    {
        private readonly IInvestmentsService _investmentsService;
        private readonly ILogger<InvestmentsController> _logger;

        public InvestmentsController(IInvestmentsService investmentsService, ILogger<InvestmentsController> logger)
        {
            _investmentsService = investmentsService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetInvestments()
        {
            var ids = UserClaimsHelper.GetUserIdAndOrgId(User);
            if (ids == null) return Unauthorized();
            var (_, orgId) = ids.Value;
            var investments = await _investmentsService.GetInvestmentsAsync(orgId);
            return Ok(investments);
        }
    }
}
