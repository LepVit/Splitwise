using FluentValidation;
using Splitwise.Core.Dtos;

namespace Splitwise.Core.Validators;

public class SplitDtoValidator : AbstractValidator<SplitDto>
{
    public SplitDtoValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.PaidShare).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true);
        RuleFor(x => x.OwedShare).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, true);
    }
}

public class CreateExpenseDtoValidator : AbstractValidator<CreateExpenseDto>
{
    public CreateExpenseDtoValidator()
    {
        RuleFor(x => x.Description).Cascade(CascadeMode.Stop).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TotalCost).GreaterThan(0).PrecisionScale(18, 2, true);
        RuleFor(x => x.GroupId).GreaterThan(0).When(x => x.GroupId.HasValue);

        RuleFor(x => x.Splits)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(splits => splits.Select(s => s.UserId).Distinct().Count() == splits.Count)
            .WithMessage("A user can appear in the splits only once.");

        RuleForEach(x => x.Splits).SetValidator(new SplitDtoValidator());
    }
}

public class CreateEqualExpenseDtoValidator : AbstractValidator<CreateEqualExpenseDto>
{
    public CreateEqualExpenseDtoValidator()
    {
        RuleFor(x => x.Description).Cascade(CascadeMode.Stop).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TotalCost).GreaterThan(0).PrecisionScale(18, 2, true);
        RuleFor(x => x.GroupId).GreaterThan(0).When(x => x.GroupId.HasValue);
        RuleFor(x => x.PaidByUserId).GreaterThan(0);

        RuleFor(x => x.ParticipantUserIds).Cascade(CascadeMode.Stop).NotEmpty();
        RuleForEach(x => x.ParticipantUserIds).GreaterThan(0);
    }
}

public class CreateSettlementDtoValidator : AbstractValidator<CreateSettlementDto>
{
    public CreateSettlementDtoValidator()
    {
        RuleFor(x => x.ToUserId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0).PrecisionScale(18, 2, true);
        RuleFor(x => x.GroupId).GreaterThan(0).When(x => x.GroupId.HasValue);
    }
}
