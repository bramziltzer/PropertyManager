using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PropertyManager.AppService.Exceptions;
using PropertyManager.Domain.Exceptions;

namespace PropertyManager.Infrastructure;

public sealed class ApplicationExceptionFilter(ILogger<ApplicationExceptionFilter> logger) : IExceptionFilter
{
    // TODO determine how useful this exception filter is
    public void OnException(ExceptionContext context)
    {
        var mapping = context.Exception switch
        {
            NotFoundException exception => (StatusCodes.Status404NotFound, "Not Found", exception.Message),
            ForbiddenException exception => (StatusCodes.Status403Forbidden, "Forbidden", exception.Message),
            DomainException exception => (StatusCodes.Status422UnprocessableEntity, "Business Rule Violation", exception.Message),
            _ => default
        };

        if (mapping == default)
        {
            return;
        }

        logger.LogWarning(
            context.Exception,
            "A request failed with HTTP {StatusCode}: {Message}",
            mapping.Item1,
            mapping.Item3);

        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = mapping.Item1,
            Title = mapping.Item2,
            Detail = mapping.Item3
        })
        {
            StatusCode = mapping.Item1
        };
        context.ExceptionHandled = true;
    }
}
