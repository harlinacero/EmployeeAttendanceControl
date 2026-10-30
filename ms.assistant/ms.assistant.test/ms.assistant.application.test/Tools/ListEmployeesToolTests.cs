using Microsoft.Extensions.Logging;
using Moq;
using ms.assistant.application.HttpComunications;
using ms.assistant.application.HttpComunications.Responses;
using ms.assistant.application.Tools;
using System.Text.Json;

namespace ms.assistant.application.test.Tools
{
    public class ListEmployeesToolTests
    {
        private readonly Mock<IEmployeeApiCommunication> _employeeApi;
        private readonly Mock<ILogger<ListEmployeesTool>> _logger;
        private readonly ToolContext _context = new("admin", "Bearer admin");
        private readonly JsonElement _noInput = JsonDocument.Parse("{}").RootElement;

        public ListEmployeesToolTests()
        {
            _employeeApi = new Mock<IEmployeeApiCommunication>();
            _logger = new Mock<ILogger<ListEmployeesTool>>();
        }

        private ListEmployeesTool CreateTool() => new(_employeeApi.Object, _logger.Object);

        [Fact]
        public async Task ExecuteAsyncShouldReturnEmployeesWithCurrentAttendanceState()
        {
            // Arrange
            _employeeApi.Setup(api => api.GetAllEmployees("Bearer admin")).ReturnsAsync(
            [
                new EmployeeApiResponse { UserName = "jdoe", FirstName = "John", LastName = "Doe", LastAttendanceState = false, LastAttendance = new DateTime(2026, 10, 29), LastAttendanceNotes = "Sick" },
                new EmployeeApiResponse { UserName = "asmith", FirstName = "Ann", LastName = "Smith", LastAttendanceState = true }
            ]);

            // Act
            var result = await CreateTool().ExecuteAsync(_noInput, _context, CancellationToken.None);

            // Assert
            _employeeApi.Verify(api => api.GetAllEmployees("Bearer admin"), Times.Once);
            Assert.False(result.IsError);
            var employees = JsonDocument.Parse(result.Content).RootElement;
            Assert.Equal(2, employees.GetArrayLength());
            var first = employees[0];
            Assert.Equal("jdoe", first.GetProperty("userName").GetString());
            Assert.Equal("John", first.GetProperty("firstName").GetString());
            Assert.Equal("Doe", first.GetProperty("lastName").GetString());
            Assert.False(first.GetProperty("lastAttendanceState").GetBoolean());
            Assert.True(first.TryGetProperty("lastAttendance", out _));
            Assert.Equal("Sick", first.GetProperty("lastAttendanceNotes").GetString());
        }

        [Fact]
        public async Task ExecuteAsyncShouldReturnEmptyArrayWhenNoEmployees()
        {
            // Arrange
            _employeeApi.Setup(api => api.GetAllEmployees("Bearer admin")).ReturnsAsync([]);

            // Act
            var result = await CreateTool().ExecuteAsync(_noInput, _context, CancellationToken.None);

            // Assert
            Assert.False(result.IsError);
            Assert.Equal("[]", result.Content);
        }

        [Fact]
        public async Task ExecuteAsyncShouldReturnErrorResultWhenServiceIsUnavailable()
        {
            // Arrange
            _employeeApi.Setup(api => api.GetAllEmployees("Bearer admin")).ThrowsAsync(new HttpRequestException("down"));

            // Act
            var result = await CreateTool().ExecuteAsync(_noInput, _context, CancellationToken.None);

            // Assert
            Assert.True(result.IsError);
            Assert.Equal("The information could not be retrieved right now.", result.Content);
        }

        [Fact]
        public void ToolShouldBeAdminOnlyAndHaveNoInputs()
        {
            // Arrange
            var tool = CreateTool();

            // Act
            var schema = tool.InputSchema;

            // Assert
            Assert.Equal("list_employees", tool.Name);
            Assert.True(tool.AdminOnly);
            Assert.Empty(schema);
        }
    }
}
