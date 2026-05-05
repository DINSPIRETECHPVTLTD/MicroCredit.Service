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
            if (_userContext.UserId == 0 || _userContext.OrgId == 0 )
                return Unauthorized();
            if(!_userContext.BranchId.HasValue)
                return BadRequest("BranchId is required");
            var branches = await _loansService.GetActiveLoansAsync(_userContext.BranchId.Value);
            return Ok(branches);
        }

        [HttpPost("{id:int}/status")]
        public async Task<IActionResult> UpdateLoanStatusPost(int id, [FromBody] UpdateLoanStatusRequest request, CancellationToken cancellationToken = default)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();

            if (request == null || string.IsNullOrWhiteSpace(request.Status))
                return BadRequest("Loan status is required.");

            try
            {
                var loan = await _loansService.UpdateLoanStatusAsync(id, request.Status, _userContext.UserId, cancellationToken);
                return Ok(loan);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id:int}/claim")]
        public async Task<IActionResult> ClaimLoan(int id, CancellationToken cancellationToken = default)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();

            try
            {
                var result = await _loansService.ClaimLoanAsync(id, _userContext.UserId, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id:int}/close")]
        public async Task<IActionResult> CloseLoan(int id, [FromBody] CloseLoanRequest? request, CancellationToken cancellationToken = default)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();
            try
            {
                var result = await _loansService.CloseLoanAsync(id, _userContext.UserId, request?.ReceivedAmount, cancellationToken);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Loan close request rejected for LoanId={LoanId}", id);
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}