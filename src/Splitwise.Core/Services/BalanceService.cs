using Splitwise.Core.Common;
using Splitwise.Core.Dtos;
using Splitwise.Core.Interfaces;
using Splitwise.Core.Services.Internal;

namespace Splitwise.Core.Services;

public class BalanceService(IUnitOfWork uow) : IBalanceService
{
    public async Task<GroupBalancesDto> GetGroupBalancesAsync(int groupId, int currentUserId, CancellationToken ct = default)
    {
        var group = await GroupAccess.LoadForMemberAsync(uow, groupId, currentUserId, ct);
        var net = await uow.Expenses.GetNetBalancesByGroupAsync(groupId, ct);

        // Учасники без жодної частки мають баланс 0
        var balances = group.Members.ToDictionary(
            m => m.UserId,
            m => net.TryGetValue(m.UserId, out var value) ? decimal.Round(value, 2) : 0m);

        var users = await uow.Users.GetByIdsAsync(balances.Keys, ct);
        var names = users.ToDictionary(u => u.Id, u => u.UserName);

        var dtos = balances
            .Select(b => new UserBalanceDto(b.Key, names.GetValueOrDefault(b.Key, string.Empty), b.Value))
            .OrderByDescending(b => b.NetBalance)
            .ThenBy(b => b.UserId)
            .ToList();

        return new GroupBalancesDto(groupId, dtos, BalanceCalculator.SimplifyDebts(balances));
    }
}
