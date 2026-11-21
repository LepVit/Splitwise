using FluentValidation.TestHelper;
using Splitwise.Core.Dtos;
using Splitwise.Core.Validators;

namespace Splitwise.Tests;

public class ValidatorTests
{
    // ---------- CreateGroupDto ----------

    private readonly CreateGroupDtoValidator _group = new();

    [Theory]
    [InlineData("Admin")]
    [InlineData("system")]
    [InlineData("  ROOT ")]
    public void CreateGroup_ReservedName_Fails(string name) =>
        _group.TestValidate(new CreateGroupDto(name)).ShouldHaveValidationErrorFor(x => x.Name);

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void CreateGroup_EmptyOrTooShort_Fails(string name) =>
        _group.TestValidate(new CreateGroupDto(name)).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void CreateGroup_TooLong_Fails() =>
        _group.TestValidate(new CreateGroupDto(new string('a', 101))).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void CreateGroup_NormalName_Passes() =>
        _group.TestValidate(new CreateGroupDto("Trip to Lviv")).ShouldNotHaveAnyValidationErrors();

    // ---------- AddMemberDto ----------

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void AddMember_InvalidEmail_Fails(string email) =>
        new AddMemberDtoValidator().TestValidate(new AddMemberDto(email)).ShouldHaveValidationErrorFor(x => x.Email);

    // ---------- CreateExpenseDto ----------

    private readonly CreateExpenseDtoValidator _expense = new();

    private static CreateExpenseDto ValidExpense() =>
        new("Pizza", 100m, null, 1, [new SplitDto(1, 100m, 50m), new SplitDto(2, 0m, 50m)]);

    [Fact]
    public void CreateExpense_Valid_Passes() =>
        _expense.TestValidate(ValidExpense()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void CreateExpense_EmptySplits_Fails() =>
        _expense.TestValidate(ValidExpense() with { Splits = [] }).ShouldHaveValidationErrorFor(x => x.Splits);

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CreateExpense_NonPositiveTotal_Fails(double total) =>
        _expense.TestValidate(ValidExpense() with { TotalCost = (decimal)total }).ShouldHaveValidationErrorFor(x => x.TotalCost);

    [Fact]
    public void CreateExpense_MoreThanTwoDecimals_Fails() =>
        _expense.TestValidate(ValidExpense() with { TotalCost = 10.123m }).ShouldHaveValidationErrorFor(x => x.TotalCost);

    [Fact]
    public void CreateExpense_EmptyDescription_Fails() =>
        _expense.TestValidate(ValidExpense() with { Description = "" }).ShouldHaveValidationErrorFor(x => x.Description);

    [Fact]
    public void CreateExpense_NegativeShareInSplit_Fails()
    {
        var dto = ValidExpense() with { Splits = [new SplitDto(1, 100m, -1m)] };

        Assert.False(_expense.Validate(dto).IsValid);
    }

    [Fact]
    public void CreateExpense_DuplicateUsersInSplits_Fails()
    {
        var dto = ValidExpense() with { Splits = [new SplitDto(1, 100m, 50m), new SplitDto(1, 0m, 50m)] };

        _expense.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Splits);
    }

    // ---------- CreateEqualExpenseDto ----------

    [Fact]
    public void CreateEqualExpense_NoParticipants_Fails() =>
        new CreateEqualExpenseDtoValidator()
            .TestValidate(new CreateEqualExpenseDto("Taxi", 100m, null, 1, 1, []))
            .ShouldHaveValidationErrorFor(x => x.ParticipantUserIds);

    // ---------- CreateSettlementDto ----------

    private readonly CreateSettlementDtoValidator _settlement = new();

    [Fact]
    public void Settlement_ZeroAmount_Fails() =>
        _settlement.TestValidate(new CreateSettlementDto(2, 0m, 1)).ShouldHaveValidationErrorFor(x => x.Amount);

    [Fact]
    public void Settlement_Valid_Passes() =>
        _settlement.TestValidate(new CreateSettlementDto(2, 25.50m, null)).ShouldNotHaveAnyValidationErrors();

    // ---------- UpdateProfileDto ----------

    [Fact]
    public void UpdateProfile_TooLongName_Fails() =>
        new UpdateProfileDtoValidator()
            .TestValidate(new UpdateProfileDto(new string('a', 101), null))
            .ShouldHaveValidationErrorFor(x => x.FirstName);
}
