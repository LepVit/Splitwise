using Splitwise.Core.Dtos;

namespace Splitwise.Core.Interfaces;

public interface IGroupService
{
    Task<GroupDto> CreateAsync(CreateGroupDto dto, int currentUserId, CancellationToken ct = default);
    Task<GroupDto> GetAsync(int groupId, int currentUserId, CancellationToken ct = default);
    Task<IReadOnlyList<GroupSummaryDto>> GetMyGroupsAsync(int currentUserId, CancellationToken ct = default);
    Task<GroupDto> AddMemberAsync(int groupId, AddMemberDto dto, int currentUserId, CancellationToken ct = default);
}

public interface IExpenseService
{
    Task<ExpenseDto> CreateAsync(CreateExpenseDto dto, int currentUserId, CancellationToken ct = default);
    Task<ExpenseDto> CreateEqualAsync(CreateEqualExpenseDto dto, int currentUserId, CancellationToken ct = default);
    Task<ExpenseDto> SettleUpAsync(CreateSettlementDto dto, int currentUserId, CancellationToken ct = default);
    Task<ExpenseDto> GetByIdAsync(int expenseId, int currentUserId, CancellationToken ct = default);
    Task<IReadOnlyList<ExpenseDto>> GetGroupExpensesAsync(int groupId, int currentUserId, CancellationToken ct = default);
}

public interface IBalanceService
{
    Task<GroupBalancesDto> GetGroupBalancesAsync(int groupId, int currentUserId, CancellationToken ct = default);
}

public interface IUserService
{
    Task<UserProfileDto> GetProfileAsync(int userId, CancellationToken ct = default);
    Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileDto dto, CancellationToken ct = default);
}
