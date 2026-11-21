using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Splitwise.Core.Dtos;
using Splitwise.Core.Interfaces;

namespace Splitwise.Api.Controllers;

[Authorize]
[Route("api/groups")]
public class GroupsController(IGroupService groups) : ApiControllerBase
{
    /// <summary>Групи поточного користувача.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GroupSummaryDto>>> GetMine(CancellationToken ct) =>
        Ok(await groups.GetMyGroupsAsync(CurrentUserId, ct));

    [HttpPost]
    public async Task<ActionResult<GroupDto>> Create(CreateGroupDto dto, CancellationToken ct)
    {
        var created = await groups.CreateAsync(dto, CurrentUserId, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GroupDto>> GetById(int id, CancellationToken ct) =>
        Ok(await groups.GetAsync(id, CurrentUserId, ct));

    /// <summary>Додає учасника в групу за його Email.</summary>
    [HttpPost("{id:int}/members")]
    public async Task<ActionResult<GroupDto>> AddMember(int id, AddMemberDto dto, CancellationToken ct) =>
        Ok(await groups.AddMemberAsync(id, dto, CurrentUserId, ct));
}
