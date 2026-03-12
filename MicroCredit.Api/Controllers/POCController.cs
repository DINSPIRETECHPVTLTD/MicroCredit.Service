using MicroCredit.Domain.Common;
using MicroCredit.Domain.Interfaces.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MicroCredit.Domain.Model.Poc;

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

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            try
            {
                var poc = await _pocService.GetByIdAsync(id, cancellationToken);
                return Ok(poc);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "POC not found: {Id}", id);
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePocRequest request, CancellationToken cancellationToken)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();
            var poc = await _pocService.CreateAsync(request, _userContext, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = poc.Id }, poc);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePocRequest request, CancellationToken cancellationToken)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();
            var poc = await _pocService.UpdateAsync(id, request, _userContext, cancellationToken);
            return Ok(poc);
        }

        [HttpDelete("{id:int}/inactive")]
        public async Task<IActionResult> MarkAsInactive(int id, CancellationToken cancellationToken)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();
            var result = await _pocService.MarkAsInactiveAsync(id, _userContext.UserId, cancellationToken);
            return Ok(result);
        }
    }
}