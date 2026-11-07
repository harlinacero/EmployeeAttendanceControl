using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ms.users.domain.Exceptions;

namespace ms.users.api.Extensions
{
    /// <summary>
    /// Turns unreachable users' data into a 503 response, so it's never mistaken for wrong credentials.
    /// </summary>
    public class DataUnavailableExceptionHandler : IExceptionHandler
    {
        public const string Detail = "The authentication service is temporarily unavailable. Please try again later.";

        private readonly ILogger<DataUnavailableExceptionHandler> _logger;

        public DataUnavailableExceptionHandler(ILogger<DataUnavailableExceptionHandler> logger) => _logger = logger;

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not DataUnavailableException)
            {
                return false;
            }

            _logger.LogError(exception.InnerException, "Users' data unavailable for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
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
