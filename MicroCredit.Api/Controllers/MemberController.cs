using MicroCredit.Domain.Common;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Domain.Model.Member;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MicroCredit.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class MemberController : ControllerBase
{
    private readonly IMemberService _memberService;
    private readonly ILogger<MemberController> _logger;
    private readonly IUserContext _userContext;

    public MemberController(IMemberService memberService, ILogger<MemberController> logger, IUserContext userContext)
    {
        _memberService = memberService;
        _logger = logger;
        _userContext = userContext;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var member = await _memberService.GetByIdAsync(id, cancellationToken);
            if (member == null)
                return NotFound(new { message = "Member not found." });
            return Ok(member);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Member not found: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("by-branch/{branchId}")]
    public async Task<IActionResult> GetByBranchId(int branchId, CancellationToken cancellationToken)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();

        var members = await _memberService.GetMembersByBranchAsync(branchId, cancellationToken);
        if (members == null || !members.Any())
            return Ok(Enumerable.Empty<MemberResponse>());
        return Ok(members);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMemberRequest request, CancellationToken cancellationToken)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();
        var member = await _memberService.CreateAsync(request, _userContext, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = member.Id }, member);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMemberRequest request, CancellationToken cancellationToken)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();
        var member = await _memberService.UpdateAsync(id, request, _userContext, cancellationToken);
        return Ok(member);
    }

    [HttpDelete("{id:int}/inactive")]
    public async Task<IActionResult> MarkAsInactive(int id, CancellationToken cancellationToken)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();
        await _memberService.MarkAsInactiveAsync(id, _userContext.UserId, cancellationToken);
        return Ok();
    }
}
