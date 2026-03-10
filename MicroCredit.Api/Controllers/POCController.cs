using MicroCredit.Domain.Common;
using MicroCredit.Domain.Interfaces.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class POCController : ControllerBase
    {
        private readonly IPOCService _pocService;
        private readonly ILogger<POCController> _logger;
        private readonly IUserContext _userContext;

        public POCController(IPOCService pocService, ILogger<POCController> logger, IUserContext userContext)
        {
            _pocService = pocService;
            _logger = logger;
            _userContext = userContext;
        }

        [HttpGet("{branchId}")]
        public async Task<IActionResult> GetPOCsByBranchId(int branchId, CancellationToken cancellationToken)
        {
            var pocs = await _pocService.GetPOCsByBranchIdAsync(branchId, cancellationToken);
            if (pocs == null || !pocs.Any())
                return NotFound(new { message = "No POCs found for the given branch." });

            return Ok(pocs);
        }


    }
}
