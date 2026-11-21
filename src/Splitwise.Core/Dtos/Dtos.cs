namespace Splitwise.Core.Dtos;

// ---------- Групи ----------
public record CreateGroupDto(string Name);

public record AddMemberDto(string Email);

public record GroupMemberDto(int UserId, string UserName, string Email, string? FirstName, string? LastName, DateTime JoinedAt);

public record GroupDto(int Id, string Name, DateTime CreatedAt, IReadOnlyList<GroupMemberDto> Members);

public record GroupSummaryDto(int Id, string Name, DateTime CreatedAt, int MembersCount);

// ---------- Витрати ----------
public record SplitDto(int UserId, decimal PaidShare, decimal OwedShare);

/// <summary>Витрата з явними частками. GroupId необов'язковий.</summary>
public record CreateExpenseDto(string Description, decimal TotalCost, DateTime? Date, int? GroupId, List<SplitDto> Splits);

/// <summary>Витрата «порівну»: один платник, їли кілька людей. Копійки розподіляються без втрат.</summary>
public record CreateEqualExpenseDto(
    string Description, decimal TotalCost, DateTime? Date, int? GroupId, int PaidByUserId, List<int> ParticipantUserIds);

/// <summary>Закриття боргу: поточний користувач віддає ToUserId суму Amount.</summary>
public record CreateSettlementDto(int ToUserId, decimal Amount, int? GroupId);

public record ExpenseDto(
    int Id, string Description, decimal TotalCost, DateTime Date, int? GroupId,
    int CreatedByUserId, bool IsPayment, DateTime CreatedAt, IReadOnlyList<SplitDto> Splits);

// ---------- Баланси ----------
/// <summary>NetBalance &gt; 0 — користувачу винні, &lt; 0 — користувач винен.</summary>
public record UserBalanceDto(int UserId, string UserName, decimal NetBalance);

public record DebtDto(int FromUserId, int ToUserId, decimal Amount);

public record GroupBalancesDto(int GroupId, IReadOnlyList<UserBalanceDto> Balances, IReadOnlyList<DebtDto> SuggestedSettlements);

// ---------- Користувачі ----------
public record UserProfileDto(int Id, string Email, string UserName, string? FirstName, string? LastName);

public record UpdateProfileDto(string? FirstName, string? LastName);
