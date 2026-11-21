using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Splitwise.Core.Dtos;
using Splitwise.Core.Interfaces;

namespace Splitwise.Api.Controllers;

[Authorize]
[Route("api/expenses")]
public class ExpensesController(IExpenseService expenses) : ApiControllerBase
{
    /// <summary>Нова витрата з явними частками (PaidShare / OwedShare). GroupId необов'язковий.</summary>
    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> Create(CreateExpenseDto dto, CancellationToken ct)
    {
        var created = await expenses.CreateAsync(dto, CurrentUserId, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Нова витрата «порівну»: копійки розподіляються без втрат.</summary>
    [HttpPost("equal")]
    public async Task<ActionResult<ExpenseDto>> CreateEqual(CreateEqualExpenseDto dto, CancellationToken ct)
    {
        var created = await expenses.CreateEqualAsync(dto, CurrentUserId, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Закриття боргу (Settle Up): запис у тому самому леджері з IsPayment = true.</summary>
    [HttpPost("settle")]
    public async Task<ActionResult<ExpenseDto>> SettleUp(CreateSettlementDto dto, CancellationToken ct)
    {
        var created = await expenses.SettleUpAsync(dto, CurrentUserId, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ExpenseDto>> GetById(int id, CancellationToken ct) =>
        Ok(await expenses.GetByIdAsync(id, CurrentUserId, ct));

    [HttpGet("~/api/groups/{groupId:int}/expenses")]
    public async Task<ActionResult<IReadOnlyList<ExpenseDto>>> GetByGroup(int groupId, CancellationToken ct) =>
        Ok(await expenses.GetGroupExpensesAsync(groupId, CurrentUserId, ct));
}
