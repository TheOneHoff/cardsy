using Cardsy.API.Infrastructure.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Cardsy.API.Infrastructure.Handlers
{
    public class DevelopmentExceptionHandler() : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            const string message = "Internal Server Error";

            ProblemDetails detail = new()
            {
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                Title = message,
                Status = StatusCodes.Status500InternalServerError,
                Detail = exception.Message,
            };

            await httpContext.Response.WriteAsJsonAsync(detail, SystemJsonSerializationContext.Default.ProblemDetails, cancellationToken: cancellationToken);

            return true;
        }
    }
}
