using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using ms.users.api.Extensions;
using ms.users.domain.Exceptions;

namespace ms.users.api.test.Extensions
{
    public class DataUnavailableExceptionHandlerTests
    {
        private readonly Mock<ILogger<DataUnavailableExceptionHandler>> _loggerMock = new();
        private readonly DataUnavailableExceptionHandler _handler;

        public DataUnavailableExceptionHandlerTests() => _handler = new DataUnavailableExceptionHandler(_loggerMock.Object);

        [Fact]
        public async Task TryHandleShouldReturn503ForDataUnavailable()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            var handled = await _handler.TryHandleAsync(context, new DataUnavailableException(new Exception("down")), CancellationToken.None);

            // Assert
            Assert.True(handled);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
            _loggerMock.Verify(logger => logger.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                It.Is<Exception>(logged => logged.Message == "down"), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task TryHandleShouldIgnoreOtherExceptions()
        {
            // Arrange
            var context = new DefaultHttpContext();

            // Act
            var handled = await _handler.TryHandleAsync(context, new InvalidOperationException(), CancellationToken.None);

            // Assert
            Assert.False(handled);
        }
    }
}
