using MicroCredit.Api.Helpers;
using MicroCredit.Domain.Common;
using MicroCredit.Domain.Interfaces.Services;
using MicroCredit.Domain.Model.Loan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class LoansController : ControllerBase
    {
        private readonly ILoansService _loansService;
        private readonly ILogger<LoansController> _logger;

        public LoansController(ILoansService loansService, ILogger<LoansController> logger)
        {
            _loansService = loansService;
            _logger = logger;
        }

        // GET: api/Loans
        [HttpGet]
        public async Task<IActionResult> GetLoans()
        {
            var loans = await _loansService.GetAllAsync();
            return Ok(loans);
        }

        [HttpGet("MemberId/{memberId:int}")]
        public async Task<IActionResult> GetLoanByMemId(int memberId, CancellationToken cancellationToken =default)
        {
            var loans = await _loansService.GetLoanByMemId(memberId, cancellationToken);
            return Ok(loans);
        }

        [HttpPost("Add-Loan")]
        public async Task<IActionResult> CreateLoan([FromBody] CreateLoanRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || request.LoanAmount <= 0)
                return BadRequest("Valid loan amount is required.");
            var ids = UserClaimsHelper.GetUserIdAndOrgId(User);
            if (ids == null) return Unauthorized();
            var (userId, _) = ids.Value;
            var loan = await _loansService.AddLoanAsync(request, userId, cancellationToken);
            return Ok(loan.Id);
        }

        [HttpGet("ActiveLoans")]       
        public async Task<IActionResult> GetActiveLoans()
        {
            var loans = await _loansService.GetAllAsync();
            return Ok(loans);
        }
    }
}