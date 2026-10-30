using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ms.assistant.api.Extensions;
using ms.assistant.domain.Exceptions;

namespace ms.assistant.api.test.Extensions
{
    public class AssistantUnavailableExceptionHandlerTests
    {
        private static DefaultHttpContext CreateContext()
        {
            var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
            return new DefaultHttpContext { RequestServices = services, Response = { Body = new MemoryStream() } };
        }

        [Fact]
        public async Task TryHandleAsyncShouldWrite503WhenModelIsUnavailable()
        {
            // Arrange
            var context = CreateContext();
            var handler = new AssistantUnavailableExceptionHandler();

            // Act
            var handled = await handler.TryHandleAsync(context, new AssistantModelUnavailableException("down", new HttpRequestException()), CancellationToken.None);

            // Assert
            Assert.True(handled);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
            context.Response.Body.Position = 0;
            var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
            Assert.Contains(AssistantUnavailableExceptionHandler.Detail, body);
        }

        [Fact]
        public async Task TryHandleAsyncShouldIgnoreOtherExceptions()
        {
            // Arrange
            var context = CreateContext();
            var handler = new AssistantUnavailableExceptionHandler();

            // Act
            var handled = await handler.TryHandleAsync(context, new InvalidOperationException(), CancellationToken.None);

            // Assert
            Assert.False(handled);
        }
    }
}
