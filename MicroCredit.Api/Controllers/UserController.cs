using Azure;
using MicroCredit.Application.Interfaces;
using MicroCredit.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MicroCredit.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public UserController(IUserService userService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllOrgUsers()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var orgIdClaim = User.FindFirstValue("OrgId");
            if (string.IsNullOrEmpty(orgIdClaim) || !int.TryParse(orgIdClaim, out var orgId))
                return Unauthorized();
            var users = await _userService.GetOrgUsersAsync(orgId);
            return Ok(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBranchUsers()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var orgIdClaim = User.FindFirstValue("OrgId");
            if (string.IsNullOrEmpty(orgIdClaim) || !int.TryParse(orgIdClaim, out var orgId))
                return Unauthorized();

            var branchIdClaim = User.FindFirstValue("BranchId");
            if (string.IsNullOrEmpty(orgIdClaim) || !int.TryParse(orgIdClaim, out var branchId))
                return Unauthorized();

            var users = await _userService.GetBranchUsersAsync(orgId, branchId);
            return Ok(users);
        }
    }
}
