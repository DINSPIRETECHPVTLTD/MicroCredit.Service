using MicroCredit.Api.Abstractions;
using MicroCredit.Application.Services;
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
        private readonly IUserContext _userContext;

        public LoansController(ILoansService loansService, ILogger<LoansController> logger, IUserContext userContext)
        {
            _loansService = loansService;
            _logger = logger;
            _userContext = userContext;
        }

        // GET: api/Loans
        [HttpGet]
        public async Task<IActionResult> GetLoans()
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();
            var loans = await _loansService.GetAllAsync();
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
            return Ok(loan);
        }

        [HttpGet("ActiveLoans")]       
        public async Task<IActionResult> GetActiveLoans()
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0 )
                return Unauthorized();
            if(!_userContext.BranchId.HasValue)
                return BadRequest("BranchId is required");
            var branches = await _loansService.GetActiveLoansAsync(_userContext.BranchId.Value);
            return Ok(branches);
        }
    }
}