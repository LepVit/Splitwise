using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Splitwise.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Id поточного користувача береться лише з токена (Claims), ніколи з тіла запиту.</summary>
    protected int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new UnauthorizedAccessException("User id claim is missing or invalid.");
}
