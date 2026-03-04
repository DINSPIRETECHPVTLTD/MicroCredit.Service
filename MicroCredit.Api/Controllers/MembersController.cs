using MicroCredit.Api.Helpers;
using MicroCredit.Domain.Interfaces.Services;
using MicroCredit.Domain.Model.Member;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers;

[Route("Members")]
[ApiController]
[Authorize]

public class MembersController : ControllerBase
{
    private readonly IMembersService _membersService;
    private readonly ILogger<MembersController> _logger;

    public MembersController(IMembersService membersService, ILogger<MembersController> logger)
    {
        _membersService = membersService;
        _logger = logger;
    }
    [HttpGet("org")]
    public async Task<IActionResult> GetMembers(CancellationToken cancellationToken)
    {
        var ids = UserClaimsHelper.GetUserIdOrgIdAndBranchId(User);
        if (ids == null) return Unauthorized();
        var (_, _, branchId) = ids.Value;
        if (!branchId.HasValue) return Unauthorized();
        var members = await _membersService.GetMembersAsync(branchId.Value, cancellationToken);
        return Ok(members);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetMember(int id, CancellationToken cancellationToken)
    {
        var member = await _membersService.GetMemberAsync(id, cancellationToken);
        if (member == null) return NotFound();
        return Ok(member);
    }

    [HttpPost]
    public async Task<IActionResult> CreateMember([FromBody] MemberRequest request, CancellationToken cancellationToken)
    {
        var created = await _membersService.CreateMemberAsync(request, cancellationToken);
        // For now return Created with the created resource
        return CreatedAtAction(nameof(GetMember), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateMember(int id, [FromBody] MemberRequest request, CancellationToken cancellationToken)
    {
        var updated = await _membersService.UpdateMemberAsync(id, request, cancellationToken);
        return Ok(updated);
    }
}
