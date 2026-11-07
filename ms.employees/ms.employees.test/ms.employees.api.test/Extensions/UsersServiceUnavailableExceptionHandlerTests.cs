using Microsoft.AspNetCore.Http;
using ms.employees.api.Extensions;
using ms.employees.domain.Exceptions;

namespace ms.employees.api.test.Extensions
{
    public class UsersServiceUnavailableExceptionHandlerTests
    {
        private readonly UsersServiceUnavailableExceptionHandler _handler = new();

        [Fact]
        public async Task TryHandleShouldReturn503()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            var handled = await _handler.TryHandleAsync(context, new UsersServiceUnavailableException(new HttpRequestException()), CancellationToken.None);

            // Assert
            Assert.True(handled);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        }

        [Fact]
        public async Task TryHandleShouldIgnoreOtherExceptions()
        {
            // Act
            var handled = await _handler.TryHandleAsync(new DefaultHttpContext(), new InvalidOperationException(), CancellationToken.None);

            // Assert
            Assert.False(handled);
        }
    }
}
