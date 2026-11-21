using FluentValidation;
using Splitwise.Core.Dtos;

namespace Splitwise.Core.Validators;

public class CreateGroupDtoValidator : AbstractValidator<CreateGroupDto>
{
    private static readonly string[] ReservedGroupNames = ["Admin", "System", "Root"];

    public CreateGroupDtoValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Length(3, 100)
            .Must(name => !ReservedGroupNames.Contains(name.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Group name cannot be a reserved system name.");
    }
}

public class AddMemberDtoValidator : AbstractValidator<AddMemberDto>
{
    public AddMemberDtoValidator()
    {
        RuleFor(x => x.Email).Cascade(CascadeMode.Stop).NotEmpty().EmailAddress().MaximumLength(256);
    }
}
