using MicroCredit.Api.Helpers;
using MicroCredit.Application.Interfaces;
using MicroCredit.Application.Model.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Email and password are required.");

        var response = await _authService.LoginAsync(request, cancellationToken);

        if (response == null)
            return Unauthorized("Invalid email or password.");

        return Ok(response);
    }

    [Authorize]
    [HttpPost("navigate-to-branch")]
    public async Task<IActionResult> NavigateToBranch([FromQuery] int branchId, CancellationToken cancellationToken = default)
    {
        var userId = UserClaimsHelper.GetUserId(User);
        if (userId == null) return Unauthorized();

        var response = await _authService.NavigateToBranchAsync(userId.Value, branchId, cancellationToken);
        if (response == null)
            return BadRequest("Branch not found or you do not have access to it.");

        return Ok(response);
    }

    [Authorize]
    [HttpPost("navigate-to-org")]
    public async Task<IActionResult> NavigateToOrg(CancellationToken cancellationToken = default)
    {
        var userId = UserClaimsHelper.GetUserId(User);
        if (userId == null) return Unauthorized();

        var response = await _authService.NavigateToOrgAsync(userId.Value, cancellationToken);
        if (response == null)
            return Unauthorized("User not found.");

        return Ok(response);
    }
}
