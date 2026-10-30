using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ms.assistant.domain.Exceptions;

namespace ms.assistant.api.Extensions
{
    /// <summary>
    /// Turns an unavailable AI model into a 503 response instead of a 500.
    /// </summary>
    public class AssistantUnavailableExceptionHandler : IExceptionHandler
    {
        public const string Detail = "The assistant is temporarily unavailable. Please try again later.";

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not AssistantModelUnavailableException)
            {
                return false;
            }

            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Service unavailable",
                Detail = Detail
            }, cancellationToken);
            return true;
        }
    }
}
