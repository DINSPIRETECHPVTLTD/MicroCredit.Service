using MicroCredit.Domain.Common;
using MicroCredit.Domain.Entities;
using MicroCredit.Domain.Interfaces.Service;
using MicroCredit.Api.Helpers;
using MicroCredit.Domain.Model.Member;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        var role = UserClaimsHelper.GetUserRole(User);
        if (role == UserRole.BranchAdmin || role == UserRole.Staff)
        {
            if (!_userContext.BranchId.HasValue)
                return StatusCode(StatusCodes.Status403Forbidden, "Branch context is required.");
            if (_userContext.BranchId.Value != branchId)
                return StatusCode(StatusCodes.Status403Forbidden, "You can access only your branch data.");
        }

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
        try
        {
            var member = await _memberService.CreateAsync(request, _userContext, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = member.Id }, member);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation failed while creating member.");
            return BadRequest(new { message = ex.Message });
        }
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

    [HttpPost("by-branch/search-member")]
    public async Task<IActionResult> GetByBranchId([FromBody] SearchMemberRequest request,  CancellationToken cancellationToken)
    {
        if (_userContext.UserId == 0 || _userContext.OrgId == 0)
            return Unauthorized();
        var role = UserClaimsHelper.GetUserRole(User);
        if (role == UserRole.BranchAdmin || role == UserRole.Staff)
        {
            if (!_userContext.BranchId.HasValue)
                return StatusCode(StatusCodes.Status403Forbidden, "Branch context is required.");
            request.BranchId = _userContext.BranchId.Value;
        }

        var members = await _memberService.SearchMemebersByBranchAsync(request, cancellationToken);
        if (members == null || !members.Any())
            return Ok(Enumerable.Empty<MemberResponse>());
        return Ok(members);
    }
}
