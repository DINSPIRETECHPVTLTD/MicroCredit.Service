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

    }
}