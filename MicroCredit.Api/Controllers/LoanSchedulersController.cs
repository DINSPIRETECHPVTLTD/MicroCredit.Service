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
    public class LoanSchedulersController : ControllerBase
    {
        private readonly IUserContext _userContext;
        private readonly ILoanSchedulerService _loanSchedulerService;

        public LoanSchedulersController(ILoanSchedulerService loanSchedulerService, IUserContext userContext)
        {
            _loanSchedulerService = loanSchedulerService;
            _userContext = userContext;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetLoanSchedulersByIdAsync(int id,CancellationToken cancellationToken)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();
            var response = await _loanSchedulerService.GetLoanSchedulersByIdAsync(id, cancellationToken);
            return Ok(response);
        }
    }
}
