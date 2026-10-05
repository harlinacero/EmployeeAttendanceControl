using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ms.employees.api.Controllers;
using ms.employees.application.Commands;
using ms.employees.application.Queries;
using ms.employees.application.Requests;
using ms.employees.application.Responses;

namespace ms.employees.api.test.Controllers
{
    public class EmployeesControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;

        public EmployeesControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
        }

        [Fact]
        public async Task GetAllEmployeesShouldReturnOkResultWithEmployeeResponses()
        {
            // Arrange
            var employeeResponses = new List<EmployeeResponse>
            {
                new() { UserName = "user1", FirstName = "John", LastName = "Doe" },
                new() { UserName = "user2", FirstName = "Jane", LastName = "Smith" }
            };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllEmployeesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(employeeResponses);
            var controller = new EmployeesController(_mediatorMock.Object);
            
            // Act
            var result = await controller.GetAllEmployees();
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedEmployeeResponses = Assert.IsType<IEnumerable<EmployeeResponse>>(okResult.Value, exactMatch: false);
            Assert.Equal(employeeResponses, returnedEmployeeResponses);
        }

        [Fact]
        public async Task CreateEmployeeShouldReturnOkResultWithCreatedEmployeeResponse()
        {
            // Arrange
            var createEmployeeRequest = new CreateEmployeeRequest
            {
                UserName = "newuser",
                FirstName = "New",
                LastName = "User",
                Password = "password",
                Role = "employee"
            };
            var createdEmployeeResponse = new EmployeeResponse
            {
                UserName = createEmployeeRequest.UserName,
                FirstName = createEmployeeRequest.FirstName,
                LastName = createEmployeeRequest.LastName
            };
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateEmployeeCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdEmployeeResponse.UserName);
            var controller = new EmployeesController(_mediatorMock.Object);
            
            // Act
            var result = await controller.CreateEmployee(createEmployeeRequest);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedEmployeeResponse = Assert.IsType<string>(okResult.Value);
            Assert.Equal(createdEmployeeResponse.UserName, okResult.Value);
        }

        [Fact]
        public async Task UpdateAttendanceStateShouldReturnOkResultWithUpdatedEmployeeResponse()
        {
            // Arrange
            var username = "testuser";
            var attendance = true;
            var notes = "Present";

            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateAttendanceStateCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(username);
            var controller = new EmployeesController(_mediatorMock.Object);
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(
                    new System.Security.Claims.ClaimsIdentity(
                        [new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, username)], "TestAuth"))
            };
            httpContext.Request.Headers.Authorization = "Bearer testtoken";
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.UpdateAttendanceState(attendance, notes);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedEmployeeResponse = Assert.IsType<string>(okResult.Value);
            Assert.Equal(username, okResult.Value);
        }
    }
}
