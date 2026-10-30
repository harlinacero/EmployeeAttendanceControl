using Microsoft.Extensions.Logging;
using Moq;
using ms.assistant.application.HttpComunications;
using ms.assistant.application.HttpComunications.Responses;
using ms.assistant.application.Tools;
using System.Text.Json;

namespace ms.assistant.application.test.Tools
{
    public class GetMyAttendancesToolTests
    {
        private readonly Mock<IAttendanceApiCommunication> _attendanceApi;
        private readonly Mock<ILogger<GetMyAttendancesTool>> _logger;
        private readonly ToolContext _context = new("jdoe", "Bearer abc");

        public GetMyAttendancesToolTests()
        {
            _attendanceApi = new Mock<IAttendanceApiCommunication>();
            _logger = new Mock<ILogger<GetMyAttendancesTool>>();
        }

        private GetMyAttendancesTool CreateTool() => new(_attendanceApi.Object, _logger.Object);

        private static JsonElement Input(string json) => JsonDocument.Parse(json).RootElement;

        [Fact]
        public async Task ExecuteAsyncShouldQueryCallerAttendancesWithCallerToken()
        {
            // Arrange
            _attendanceApi.Setup(api => api.GetAllAttendances("jdoe", "Bearer abc"))
                .ReturnsAsync([new AttendanceApiResponse { UserName = "jdoe", Date = new DateTime(2026, 10, 3), Attendance = false, Notes = "Sick" }]);

            // Act
            var result = await CreateTool().ExecuteAsync(Input("{}"), _context, CancellationToken.None);

            // Assert
            _attendanceApi.Verify(api => api.GetAllAttendances("jdoe", "Bearer abc"), Times.Once);
            Assert.False(result.IsError);
            Assert.Contains("Sick", result.Content);
        }

        [Fact]
        public async Task ExecuteAsyncShouldIgnoreUserNameInInput()
        {
            // Arrange
            _attendanceApi.Setup(api => api.GetAllAttendances(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync([]);

            // Act
            await CreateTool().ExecuteAsync(Input("""{"userName":"boss"}"""), _context, CancellationToken.None);

            // Assert
            _attendanceApi.Verify(api => api.GetAllAttendances("jdoe", "Bearer abc"), Times.Once);
            _attendanceApi.Verify(api => api.GetAllAttendances("boss", It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteAsyncShouldReturnEmptyArrayWhenNoAttendances()
        {
            // Arrange
            _attendanceApi.Setup(api => api.GetAllAttendances("jdoe", "Bearer abc")).ReturnsAsync([]);

            // Act
            var result = await CreateTool().ExecuteAsync(Input("{}"), _context, CancellationToken.None);

            // Assert
            Assert.False(result.IsError);
            Assert.Equal("[]", result.Content);
        }

        [Fact]
        public async Task ExecuteAsyncShouldReturnErrorResultWhenServiceIsUnavailable()
        {
            // Arrange
            var exception = new HttpRequestException("connection refused");
            _attendanceApi.Setup(api => api.GetAllAttendances("jdoe", "Bearer abc")).ThrowsAsync(exception);

            // Act
            var result = await CreateTool().ExecuteAsync(Input("{}"), _context, CancellationToken.None);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal("The information could not be retrieved right now.", result.Content);
            _logger.Verify(logger => logger.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void ToolShouldBeAvailableToEveryoneAndHaveNoInputs()
        {
            // Arrange
            var tool = CreateTool();

            // Act
            var schema = tool.InputSchema;

            // Assert
            Assert.Equal("get_my_attendances", tool.Name);
            Assert.False(tool.AdminOnly);
            Assert.Empty(schema);
            Assert.Empty(tool.Required);
        }
    }
}
