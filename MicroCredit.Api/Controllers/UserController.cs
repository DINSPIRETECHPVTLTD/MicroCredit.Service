using MicroCredit.Api.Helpers;
using MicroCredit.Application.Interfaces;
using MicroCredit.Application.Model.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers;

[Route("[controller]")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("Org")]
    public async Task<IActionResult> GetAllOrgUsers()
    {
        var ids = UserClaimsHelper.GetUserIdAndOrgId(User);
        if (ids == null) return Unauthorized();
        var (_, orgId) = ids.Value;
        var users = await _userService.GetOrgUsersAsync(orgId);
        return Ok(users);
    }

    [HttpGet("Branch")]
    public async Task<IActionResult> GetAllBranchUsers()
    {
        var ids = UserClaimsHelper.GetUserIdOrgIdAndBranchId(User);
        if (ids == null) return Unauthorized();
        var (_, orgId, branchId) = ids.Value;
        if (!branchId.HasValue) return BadRequest("Branch context is required.");
        var users = await _userService.GetBranchUsersAsync(orgId, branchId.Value);
        return Ok(users);
    }

    [HttpPost()]
    public async Task<IActionResult> Create(CreateUserResponse response, CancellationToken cancellationToken)
    {
        var ids = UserClaimsHelper.GetUserIdOrgIdAndBranchId(User);
        if (ids == null) return Unauthorized();
        var (userId, orgId, branchId) = ids.Value;
        var users = await _userService.CreateUserAsync(response, orgId, branchId, userId, cancellationToken);
        return Ok(users);
    }

    [HttpPut()]
    public async Task<IActionResult> Update(UpdateUserResponse response, CancellationToken cancellationToken)
    {
        var ids = UserClaimsHelper.GetUserIdOrgIdAndBranchId(User);
        if (ids == null) return Unauthorized();
        var (userId, orgId, branchId) = ids.Value;
        var users = await _userService.UpdateUserAsync(response, orgId, branchId, userId, cancellationToken);
        return Ok(users);
    }
}
