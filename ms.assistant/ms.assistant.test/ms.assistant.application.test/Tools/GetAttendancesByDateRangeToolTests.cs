using Microsoft.Extensions.Logging;
using Moq;
using ms.assistant.application.HttpComunications;
using ms.assistant.application.HttpComunications.Responses;
using ms.assistant.application.Tools;
using System.Text.Json;

namespace ms.assistant.application.test.Tools
{
    public class GetAttendancesByDateRangeToolTests
    {
        private readonly Mock<IAttendanceApiCommunication> _attendanceApi;
        private readonly Mock<ILogger<GetAttendancesByDateRangeTool>> _logger;
        private readonly ToolContext _context = new("admin", "Bearer admin");

        public GetAttendancesByDateRangeToolTests()
        {
            _attendanceApi = new Mock<IAttendanceApiCommunication>();
            _logger = new Mock<ILogger<GetAttendancesByDateRangeTool>>();
        }

        private GetAttendancesByDateRangeTool CreateTool() => new(_attendanceApi.Object, _logger.Object);

        private static JsonElement Input(string userName, string from, string to)
            => JsonSerializer.SerializeToElement(new { userName, from, to });

        private void VerifyServiceNeverCalled()
            => _attendanceApi.Verify(api => api.GetAttendancesByDateRange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        [Fact]
        public async Task ExecuteAsyncShouldQueryRequestedEmployeeAndRangeWithCallerToken()
        {
            // Arrange
            _attendanceApi.Setup(api => api.GetAttendancesByDateRange("jdoe", "2026-10-01", "2026-10-31", "Bearer admin"))
                .ReturnsAsync([new AttendanceApiResponse { UserName = "jdoe", Notes = "Doctor" }]);

            // Act
            var result = await CreateTool().ExecuteAsync(Input("jdoe", "2026-10-01", "2026-10-31"), _context, CancellationToken.None);

            // Assert
            _attendanceApi.Verify(api => api.GetAttendancesByDateRange("jdoe", "2026-10-01", "2026-10-31", "Bearer admin"), Times.Once);
            Assert.False(result.IsError);
            Assert.Contains("Doctor", result.Content);
        }

        [Fact]
        public async Task ExecuteAsyncShouldReturnErrorWithoutCallingServiceWhenRangeExceeds31Days()
        {
            // Act
            var result = await CreateTool().ExecuteAsync(Input("jdoe", "2026-10-01", "2026-11-01"), _context, CancellationToken.None);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal("The date range cannot exceed 31 days.", result.Content);
            VerifyServiceNeverCalled();
        }

        [Fact]
        public async Task ExecuteAsyncShouldReturnErrorWithoutCallingServiceWhenFromIsLaterThanTo()
        {
            // Act
            var result = await CreateTool().ExecuteAsync(Input("jdoe", "2026-10-10", "2026-10-01"), _context, CancellationToken.None);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal("The start date cannot be later than the end date.", result.Content);
            VerifyServiceNeverCalled();
        }

        [Fact]
        public async Task ExecuteAsyncShouldReturnErrorWhenDateIsInvalid()
        {
            // Act
            var result = await CreateTool().ExecuteAsync(Input("jdoe", "2026-02-30", "2026-03-05"), _context, CancellationToken.None);

            // Assert
            Assert.True(result.IsError);
            Assert.Contains("from", result.Content);
            VerifyServiceNeverCalled();
        }

        [Fact]
        public async Task ExecuteAsyncShouldReturnErrorWhenInputFieldIsMissing()
        {
            // Arrange
            var input = JsonSerializer.SerializeToElement(new { from = "2026-10-01", to = "2026-10-31" });

            // Act
            var result = await CreateTool().ExecuteAsync(input, _context, CancellationToken.None);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal("Invalid input: userName", result.Content);
            VerifyServiceNeverCalled();
        }

        [Fact]
        public async Task ExecuteAsyncShouldReturnEmptyArrayWhenNoAttendances()
        {
            // Arrange
            _attendanceApi.Setup(api => api.GetAttendancesByDateRange("ghost", "2026-10-01", "2026-10-31", "Bearer admin")).ReturnsAsync([]);

            // Act
            var result = await CreateTool().ExecuteAsync(Input("ghost", "2026-10-01", "2026-10-31"), _context, CancellationToken.None);

            // Assert
            Assert.False(result.IsError);
            Assert.Equal("[]", result.Content);
        }

        [Fact]
        public async Task ExecuteAsyncShouldReturnErrorResultWhenServiceIsUnavailable()
        {
            // Arrange
            var exception = new TaskCanceledException("timeout");
            _attendanceApi.Setup(api => api.GetAttendancesByDateRange("jdoe", "2026-10-01", "2026-10-31", "Bearer admin")).ThrowsAsync(exception);

            // Act
            var result = await CreateTool().ExecuteAsync(Input("jdoe", "2026-10-01", "2026-10-31"), _context, CancellationToken.None);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal("The information could not be retrieved right now.", result.Content);
            _logger.Verify(logger => logger.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public void ToolShouldBeAdminOnlyAndRequireUserNameFromAndTo()
        {
            // Arrange
            var tool = CreateTool();

            // Act
            var required = tool.Required;

            // Assert
            Assert.Equal("get_attendances_by_date_range", tool.Name);
            Assert.True(tool.AdminOnly);
            Assert.Equal(["userName", "from", "to"], required);
            Assert.Equal(["userName", "from", "to"], tool.InputSchema.Keys);
        }
    }
}
