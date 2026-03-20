using MicroCredit.Api.Abstractions;
using MicroCredit.Application.Services;
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