
using MicroCredit.Domain.Common;
using MicroCredit.Domain.Interfaces.Services;
using MicroCredit.Domain.Model.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers;

[Route("users")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUsersService _userService;
    private readonly IUserContext _userContext;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUsersService userService, IUserContext userContext, ILogger<UsersController> logger)
    {
        _userService = userService;
        _userContext = userContext;
        _logger = logger;
    }

    [HttpGet("org")]
    public async Task<IActionResult> GetOrgUsers(CancellationToken cancellationToken)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();
        var users = await _userService.GetOrgUsersAsync(_userContext.OrgId, cancellationToken);
        return Ok(users);
    }

    [HttpGet("branch")]
    public async Task<IActionResult> GetBranchUsers(CancellationToken cancellationToken)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();
        try
        {
            var (orgId, branchId) = _userContext.GetBranchContext();
            var users = await _userService.GetBranchUsersAsync(orgId, branchId, cancellationToken);
            return Ok(users);
        }
        catch (InvalidOperationException)
        {
            return BadRequest("Branch context is required. Navigate to a branch first.");
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        if (user == null)
            return NotFound();
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();
        var user = await _userService.CreateUserAsync(request, _userContext, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();
        var user = await _userService.UpdateUserAsync(id, request, _userContext, cancellationToken);
        return Ok(user);
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();
        var result = await _userService.ResetPassword(id, request.Password, _userContext.UserId, cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id:int}/inactive")]
    public async Task<IActionResult> MarkAsInactive(int id, CancellationToken cancellationToken)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();
        var result = await _userService.MarkAsInactive(id, _userContext.UserId, cancellationToken);

        return Ok(result);
    }
}
