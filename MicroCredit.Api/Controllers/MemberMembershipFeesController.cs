using MicroCredit.Domain.Common;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.MemberMembershipFee;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class MemberMembershipFeesController : ControllerBase
{
    private readonly IMemberMembershipFeeService _feeService;
    private readonly IUserContext _userContext;

    public MemberMembershipFeesController(IMemberMembershipFeeService feeService, IUserContext userContext)
    {
        _feeService = feeService;
        _userContext = userContext;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMemberMembershipFeeRequest request, CancellationToken cancellationToken)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();

        var fee = await _feeService.CreateAsync(request, _userContext, cancellationToken);
        return Ok(fee);
    }
}

