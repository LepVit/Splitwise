using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Splitwise.Core.Dtos;
using Splitwise.Core.Interfaces;

namespace Splitwise.Api.Controllers;

[Authorize]
[Route("api/users")]
public class UsersController(IUserService users) : ApiControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMe(CancellationToken ct) =>
        Ok(await users.GetProfileAsync(CurrentUserId, ct));

    /// <summary>Реєстрація приймає лише Email і пароль, ім'я та прізвище додаються тут.</summary>
    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateProfileDto dto, CancellationToken ct) =>
        Ok(await users.UpdateProfileAsync(CurrentUserId, dto, ct));
}
