using MicroCredit.Api.Helpers;
using MicroCredit.Domain.Interfaces.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers
{
    [Route("ledger-transactions")]
    [ApiController]
    [Authorize]
    public class LedgerTransactionsController : ControllerBase
    {
        private readonly ILedgerTransactionService _ledgerTransactionService;
        private readonly ILogger<LedgerTransactionsController> _logger;

        public LedgerTransactionsController(ILedgerTransactionService ledgerTransactionService, ILogger<LedgerTransactionsController> logger)
        {
            _ledgerTransactionService = ledgerTransactionService;
            _logger = logger;
        }

        [HttpGet("Expenses")]
        public async Task<IActionResult> GetExpenses()
        {
            var ids = UserClaimsHelper.GetUserIdAndOrgId(User);
            if (ids == null) return Unauthorized();
            var (_, orgId) = ids.Value;
            var expenses = await _ledgerTransactionService.GetExpensesAsync(orgId);
            return Ok(expenses);
        }
    }
}
