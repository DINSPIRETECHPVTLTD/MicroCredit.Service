using MicroCredit.Api.Helpers;
using MicroCredit.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllOrgUsers()
    {
        var ids = UserClaimsHelper.GetUserIdAndOrgId(User);
        if (ids == null) return Unauthorized();
        var (_, orgId) = ids.Value;
        var users = await _userService.GetOrgUsersAsync(orgId);
        return Ok(users);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllBranchUsers()
    {
        var ids = UserClaimsHelper.GetUserIdOrgIdAndBranchId(User);
        if (ids == null) return Unauthorized();
        var (_, orgId, branchId) = ids.Value;
        if (!branchId.HasValue) return BadRequest("Branch context is required.");
        var users = await _userService.GetBranchUsersAsync(orgId, branchId.Value);
        return Ok(users);
    }
}
