using MicroCredit.Application.Interfaces;
using MicroCredit.Application.Model.Auth;
using MicroCredit.Application.Model.User;
using MicroCredit.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace MicroCredit.Api.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;
        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _logger = logger;
            _userService = userService;
        }
        [HttpGet("{id}/Org")]
        public async Task<ActionResult> GetOrgUserAsync(int id, CancellationToken cancellationToken = default)
        {
            var response = await _userService.GetorgUserAsync(id, cancellationToken);
            if (response == null)
                return NotFound("Not found");

            return Ok(response);
        }
    }
}
