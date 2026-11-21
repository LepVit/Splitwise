namespace Splitwise.Core.Models;

/// <summary>Мінімальні дані про користувача для шару бізнес-логіки (щоб Core не залежав від Identity).</summary>
public record UserInfo(int Id, string UserName, string Email, string? FirstName, string? LastName);
