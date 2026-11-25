using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using PropertyManager.AppService.Exceptions;
using PropertyManager.Domain.Exceptions;
using PropertyManager.Infrastructure;

namespace PropertyManager.Tests.Infrastructure;

public class ApplicationExceptionFilterTests
{
    [Theory]
    [MemberData(nameof(KnownExceptions))]
    public void OnException_MapsKnownExceptions(Exception exception, int statusCode)
    {
        var context = ExceptionContextFor(exception);
        var filter = new ApplicationExceptionFilter(NullLogger<ApplicationExceptionFilter>.Instance);

        filter.OnException(context);

        context.ExceptionHandled.Should().BeTrue();
        context.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(statusCode);
        context.Result.Should().BeOfType<ObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new ProblemDetails
            {
                Status = statusCode,
                Title = statusCode switch
                {
                    404 => "Not Found",
                    403 => "Forbidden",
                    _ => "Business Rule Violation"
                },
                Detail = exception.Message
            });
    }

    [Fact]
    public void OnException_LeavesUnexpectedExceptionsUnchanged()
    {
        var context = ExceptionContextFor(new InvalidOperationException("Unexpected"));
        var filter = new ApplicationExceptionFilter(NullLogger<ApplicationExceptionFilter>.Instance);

        filter.OnException(context);

        context.ExceptionHandled.Should().BeFalse();
        context.Result.Should().BeNull();
    }

    public static TheoryData<Exception, int> KnownExceptions => new()
    {
        { new NotFoundException("Missing"), StatusCodes.Status404NotFound },
        { new ForbiddenException("Denied"), StatusCodes.Status403Forbidden },
        { new RemovalBlockedException("In use"), StatusCodes.Status422UnprocessableEntity }
    };

    private static ExceptionContext ExceptionContextFor(Exception exception)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ExceptionContext(actionContext, [])
        {
            Exception = exception
        };
    }
}
