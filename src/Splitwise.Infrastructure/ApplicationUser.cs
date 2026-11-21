using Microsoft.AspNetCore.Identity;

namespace Splitwise.Infrastructure;

/// <summary>Користувач застосунку (ASP.NET Core Identity, ключ типу int). Email, UserName, PasswordHash — з IdentityUser.</summary>
public class ApplicationUser : IdentityUser<int>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
