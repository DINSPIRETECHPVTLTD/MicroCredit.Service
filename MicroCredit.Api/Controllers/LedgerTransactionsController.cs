using MicroCredit.Api.Helpers;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.Fund;
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

        [HttpPost("Add-Expense")]
        public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || request.Amount <= 0)
                return BadRequest("Valid amount is required.");
            if (request.PaidFromUserId <= 0)
                return BadRequest("Paid from user is required.");
            var ids = UserClaimsHelper.GetUserIdAndOrgId(User);
            if (ids == null) return Unauthorized();
            var (userId, _) = ids.Value;
            await _ledgerTransactionService.CreateExpenseAsync(request, userId, cancellationToken);
            return Ok("Expense recorded successfully.");
        }

        [HttpGet("User-Transactions/{userId:int}")]
        public async Task<IActionResult> GetTransactionsForUser(int userId, CancellationToken cancellationToken = default)
        {
            var ids = UserClaimsHelper.GetUserIdAndOrgId(User);
            if (ids == null) return Unauthorized();
            var transactions = await _ledgerTransactionService.GetTransactionsByUserIdAsync(userId, cancellationToken);
            return Ok(transactions);
        }
    }
}
