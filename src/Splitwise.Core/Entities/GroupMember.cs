namespace Splitwise.Core.Entities;

/// <summary>Зв'язок «користувач входить до групи». Ключ складений: (GroupId, UserId).</summary>
public class GroupMember
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Group? Group { get; set; }
}
