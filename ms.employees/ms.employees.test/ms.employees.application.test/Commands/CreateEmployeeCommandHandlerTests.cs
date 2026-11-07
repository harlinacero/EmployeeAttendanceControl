using Microsoft.Extensions.Logging;
using Moq;
using ms.employees.application.Commands;
using ms.employees.application.Commands.Handlers;
using ms.employees.application.HttpComunications;
using ms.employees.application.HttpComunications.Requests;
using ms.employees.domain.Entities;
using ms.employees.domain.Exceptions;
using ms.employees.domain.Repositories;
using Refit;
using System.Net;

namespace ms.employees.application.test.Commands
{
    public class CreateEmployeeCommandHandlerTests
    {
        private const string Token = "Bearer admin-token";

        private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
        private readonly Mock<IUsersApiCommunication> _usersApiCommunicationMock;
        private readonly Mock<ILogger<CreateEmployeeCommandHandler>> _loggerMock;
        private readonly CreateEmployeeCommand _command = new("testuser", "Test", "Lastname", "s3cret", "user", Token);

        public CreateEmployeeCommandHandlerTests()
        {
            _employeeRepositoryMock = new Mock<IEmployeeRepository>();
            _usersApiCommunicationMock = new Mock<IUsersApiCommunication>();
            _loggerMock = new Mock<ILogger<CreateEmployeeCommandHandler>>();

            _employeeRepositoryMock.Setup(repo => repo.CreateEmployee(It.IsAny<Employee>())).ReturnsAsync("testuser");
        }

        private Task<string> Handle() =>
            new CreateEmployeeCommandHandler(_employeeRepositoryMock.Object, _usersApiCommunicationMock.Object, _loggerMock.Object)
                .Handle(_command, CancellationToken.None);

        private static Task<ApiException> ApiError(HttpStatusCode status) =>
            ApiException.Create(new HttpRequestMessage(HttpMethod.Post, "http://ms.users.api/User"), HttpMethod.Post, new HttpResponseMessage(status), new RefitSettings());

        [Fact]
        public async Task HandleShouldCreateAccountBeforeEmployee()
        {
            // Arrange
            var calls = new List<string>();
            _usersApiCommunicationMock.Setup(api => api.CreateAccount(It.IsAny<CreateAccountApiRequest>(), It.IsAny<string>()))
                .Callback(() => calls.Add("account")).Returns(Task.CompletedTask);
            _employeeRepositoryMock.Setup(repo => repo.CreateEmployee(It.IsAny<Employee>()))
                .Callback(() => calls.Add("employee")).ReturnsAsync("testuser");

            // Act
            var result = await Handle();

            // Assert
            Assert.Equal("testuser", result);
            Assert.Equal(["account", "employee"], calls);
            _usersApiCommunicationMock.Verify(api => api.CreateAccount(new CreateAccountApiRequest("testuser", "s3cret", "user"), It.IsAny<string>()), Times.Once);
            _employeeRepositoryMock.Verify(repo => repo.CreateEmployee(It.Is<Employee>(employee =>
                employee.UserName == "testuser" && employee.FirstName == "Test" && employee.LastName == "Lastname")), Times.Once);
        }

        [Fact]
        public async Task HandleShouldForwardCallerToken()
        {
            // Act
            await Handle();

            // Assert
            _usersApiCommunicationMock.Verify(api => api.CreateAccount(It.IsAny<CreateAccountApiRequest>(), Token), Times.Once);
        }

        [Fact]
        public async Task HandleShouldContinueWhenAccountAlreadyExists()
        {
            // Arrange
            _usersApiCommunicationMock.Setup(api => api.CreateAccount(It.IsAny<CreateAccountApiRequest>(), It.IsAny<string>()))
                .ThrowsAsync(await ApiError(HttpStatusCode.Conflict));

            // Act
            await Handle();

            // Assert
            _employeeRepositoryMock.Verify(repo => repo.CreateEmployee(It.IsAny<Employee>()), Times.Once);
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("testuser") && !state.ToString()!.Contains("s3cret")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        public static TheoryData<string> UsersServiceFailures => new() { "500", "connection", "timeout" };

        [Theory]
        [MemberData(nameof(UsersServiceFailures))]
        public async Task HandleShouldNotSaveEmployeeWhenUsersServiceFails(string failure)
        {
            // Arrange
            Exception error = failure switch
            {
                "500" => await ApiError(HttpStatusCode.InternalServerError),
                "connection" => new HttpRequestException("connection refused"),
                _ => new TaskCanceledException("timeout")
            };
            _usersApiCommunicationMock.Setup(api => api.CreateAccount(It.IsAny<CreateAccountApiRequest>(), It.IsAny<string>())).ThrowsAsync(error);

            // Act & Assert
            await Assert.ThrowsAsync<UsersServiceUnavailableException>(Handle);
            _employeeRepositoryMock.Verify(repo => repo.CreateEmployee(It.IsAny<Employee>()), Times.Never);
        }

        [Fact]
        public async Task HandleShouldPropagateEmployeeSaveFailureAfterAccountCreated()
        {
            // Arrange
            _employeeRepositoryMock.Setup(repo => repo.CreateEmployee(It.IsAny<Employee>())).ThrowsAsync(new InvalidOperationException("sql down"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(Handle);
            _usersApiCommunicationMock.Verify(api => api.CreateAccount(It.IsAny<CreateAccountApiRequest>(), Token), Times.Once);
        }
    }
}
