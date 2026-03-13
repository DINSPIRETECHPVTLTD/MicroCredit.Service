using MicroCredit.Api.Helpers;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MicroCredit.Domain.Model.Fund;

namespace MicroCredit.Api.Controllers
{
    [Route("ledger-balances")]
    [ApiController]
    [Authorize]
    public class LedgerBalancesController : ControllerBase
    {
        private readonly ILedgerBalanceService _ledgerBalancesService;
        private readonly ILogger<LedgerBalancesController> _logger;

        public LedgerBalancesController(ILedgerBalanceService ledgerBalanceService, ILogger<LedgerBalancesController> logger)
        {
            _ledgerBalancesService = ledgerBalanceService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetLedgerBalances()
        {
            var ids = UserClaimsHelper.GetUserIdAndOrgId(User);
            if (ids == null) return Unauthorized();
            var (_, orgId) = ids.Value;
            var ledgerBalances = await _ledgerBalancesService.GetLedgerBalancesAsync(orgId);
            return Ok(ledgerBalances);
        }

        [HttpPost("Fund-Transfer")]
        public async Task<IActionResult> CreateFundTransfer([FromBody] CreateFundTransferRequest request, CancellationToken cancellationToken = default)
        {
            var ids = UserClaimsHelper.GetUserIdAndOrgId(User);
            if (ids == null) return Unauthorized();
            var (userId, orgId) = ids.Value;
            await _ledgerBalancesService.CreateFundTransferAsync(request, userId, cancellationToken);
            return Ok("Fund transfer completed successfully.");
        }
    }
}
