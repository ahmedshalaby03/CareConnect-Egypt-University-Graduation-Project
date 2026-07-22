using CareConnect.Application.Common.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CareConnect.Api.Filters;

/// <summary>
/// Runs the FluentValidation validator registered for any action argument before the action
/// executes, so controllers never have to start with a ModelState check.
/// </summary>
public class FluentValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _services;

    public FluentValidationFilter(IServiceProvider services) => _services = services;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new List<string>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (_services.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                errors.AddRange(result.Errors.Select(e => e.ErrorMessage));
            }
        }

        // Model binding failures (a malformed JSON number, for example) never reach a
        // validator, so they are folded in here too.
        if (!context.ModelState.IsValid)
        {
            errors.AddRange(context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                    ? "The request body could not be read."
                    : e.ErrorMessage));
        }

        if (errors.Count > 0)
        {
            context.Result = new BadRequestObjectResult(
                ApiResponse.Fail("Validation failed.", errors.Distinct().ToList()));
            return;
        }

        await next();
    }
}
