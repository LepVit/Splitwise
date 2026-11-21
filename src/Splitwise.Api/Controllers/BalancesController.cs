using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Splitwise.Core.Dtos;
using Splitwise.Core.Interfaces;

namespace Splitwise.Api.Controllers;

[Authorize]
[Route("api/balances")]
public class BalancesController(IBalanceService balances) : ApiControllerBase
{
    /// <summary>Баланси учасників групи та мінімальний набір переказів, що закриває всі борги.</summary>
    [HttpGet("~/api/groups/{groupId:int}/balances")]
    public async Task<ActionResult<GroupBalancesDto>> GetGroupBalances(int groupId, CancellationToken ct) =>
        Ok(await balances.GetGroupBalancesAsync(groupId, CurrentUserId, ct));
}
