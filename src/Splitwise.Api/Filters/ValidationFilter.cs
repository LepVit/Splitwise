using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;
using ValidationException = Splitwise.Core.Exceptions.ValidationException;

namespace Splitwise.Api.Filters;

/// <summary>
/// Перед виконанням екшену проганяє кожен аргумент через зареєстрований для нього FluentValidation-валідатор.
/// Якщо є помилки — кидає ValidationException, який GlobalExceptionHandler перетворить на 400.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument), context.HttpContext.RequestAborted);

            if (result.IsValid)
                continue;

            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

            throw new ValidationException(errors);
        }

        await next();
    }
}
