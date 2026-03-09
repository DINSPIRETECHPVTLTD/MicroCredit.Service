using MicroCredit.Domain.Common;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.Master;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentTermController : ControllerBase
    {
        private readonly IPaymentTermService _paymenttermService;
        private readonly IUserContext _userContext;
        private readonly ILogger<PaymentTermController> _logger;
        public PaymentTermController(IPaymentTermService paymenttermService, IUserContext userContext, ILogger<PaymentTermController> logger)
        {
            _paymenttermService = paymenttermService;
            _userContext = userContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentTermAsync(CancellationToken cancellationToken=default)
        {
            if (_userContext.UserId == 0 || _userContext.OrgId == 0)
                return Unauthorized();
            return Ok(await _paymenttermService.GetPaymentTermAsync(cancellationToken));
        }

    }
}
