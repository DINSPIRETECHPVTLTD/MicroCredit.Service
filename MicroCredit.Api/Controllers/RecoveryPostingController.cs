using MicroCredit.Domain.Common;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.RecoveryPosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class RecoveryPostingController : ControllerBase
{
    private readonly IRecoveryPostingService _recoveryPostingService;
    private readonly ILogger<RecoveryPostingController> _logger;
    private readonly IUserContext _userContext;

    public RecoveryPostingController(
        IRecoveryPostingService recoveryPostingService,
        ILogger<RecoveryPostingController> logger,
        IUserContext userContext)
    {
        _recoveryPostingService = recoveryPostingService;
        _logger = logger;
        _userContext = userContext;
    }

    // Loan schedulers for recovery posting with optional center and POC filters.
    [HttpGet("schedulers")]
    public async Task<IActionResult> GetSchedulers(
        [FromQuery] RecoveryPostingSchedulersRequest request,
        CancellationToken cancellationToken)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();

        if (!_userContext.BranchId.HasValue)
            return BadRequest("BranchId is required.");

        if (request == null || request.ScheduleDate == default)
            return BadRequest("ScheduleDate is required.");

        try
        {
            var result = await _recoveryPostingService.GetSchedulersAsync(request, _userContext, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Recovery posting schedulers request rejected.");
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Save recovery payments to LoanSchedulers (full or partial; partial carries shortfall to next unpaid EMI)
    [HttpPost("post")]
    public async Task<IActionResult> PostRecoveries(
        [FromBody] RecoveryPostingPostRequest request,
        CancellationToken cancellationToken)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();

        if (!_userContext.BranchId.HasValue)
            return BadRequest("BranchId is required.");

        if (request == null || request.Items == null || request.Items.Count == 0)
            return BadRequest(
                "Recovery posting requires at least one item with LoanSchedulerId, PaymentAmount, PrincipalAmount, InterestAmount, PaymentMode, and Status. Comments are optional.");

        if (request.CollectedBy <= 0)
            return BadRequest("CollectedBy is required: select the staff member who collected the payment.");

        try
        {
            var result = await _recoveryPostingService.PostRecoveriesAsync(request, _userContext, cancellationToken);
            return Ok(new {
                message = "EMI payment posted successfully.",
                postedCount = result.PostedCount
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Recovery posting post rejected.");
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
