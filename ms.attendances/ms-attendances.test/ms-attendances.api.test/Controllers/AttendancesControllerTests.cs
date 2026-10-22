using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Reflection;

using ms.attendances.api.Controllers;
using ms.attendances.application.Commands;
using ms.attendances.application.Queries;
using ms.attendances.application.Request;
using ms.attendances.application.Responses;

namespace ms_attendances.api.test.Controllers
{
    public class AttendancesControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;

        public AttendancesControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
        }

        [Fact]
        public void AttendancesController_Constructor_ShouldInitialize()
        {
            // Arrange & Act
            var controller = new AttendancesController(_mediatorMock.Object);
            // Assert
            Assert.NotNull(controller);
        }

        [Fact]
        public async Task GetAllAttendancesShouldReturnOkResult()
        {
            // Arrange
            var userName = "testUser";
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllAttendancesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AttendanceResponse>());
            var controller = new AttendancesController(_mediatorMock.Object);
            // Act
            var result = await controller.Attendances(userName);
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<List<AttendanceResponse>>(okResult.Value);
        }

        [Fact]
        public async Task CreateAttendanceShouldReturnOkResult()
        {
            // Arrange
            var createAttendanceRequest = new CreateAttendanceRequest
            {
                Attendance = true,
                Date = DateTime.UtcNow,
                FullName = "Test User",
                Notes = "Test notes"
            };

            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateAttendanceCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var controller = new AttendancesController(_mediatorMock.Object);
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = new System.Security.Claims.ClaimsPrincipal(
                    new System.Security.Claims.ClaimsIdentity(
                [
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "testUser")
                ]))
            };
            httpContext.Request.Headers.Authorization = "Bearer testtoken";            
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            
            // Act
            var result = await controller.Attendance(createAttendanceRequest);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<bool>(okResult.Value);
        }

        [Fact]
        public async Task AttendancesByDateRangeShouldReturnOkResult()
        {
            // Arrange
            var request = new GetAttendancesByDateRangeRequest { UserName = "jdoe", From = new DateOnly(2026, 3, 1), To = new DateOnly(2026, 3, 31) };
            var expectedQuery = new GetAttendancesByDateRangeQuery("jdoe", new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));
            _mediatorMock.Setup(m => m.Send(expectedQuery, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AttendanceResponse> { new() { UserName = "jdoe" } });
            var controller = new AttendancesController(_mediatorMock.Object);

            // Act
            var result = await controller.AttendancesByDateRange(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.IsType<List<AttendanceResponse>>(okResult.Value);
            _mediatorMock.Verify(m => m.Send(expectedQuery, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AttendancesByDateRangeShouldReturnOkWithEmptyListWhenNoAttendances()
        {
            // Arrange
            var request = new GetAttendancesByDateRangeRequest { UserName = "jdoe", From = new DateOnly(2026, 9, 1), To = new DateOnly(2026, 9, 30) };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAttendancesByDateRangeQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AttendanceResponse>());
            var controller = new AttendancesController(_mediatorMock.Object);

            // Act
            var result = await controller.AttendancesByDateRange(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<List<AttendanceResponse>>(okResult.Value);
            Assert.Empty(value);
        }

        [Fact]
        public void AttendancesControllerShouldRequireAuthentication()
        {
            // Arrange & Act
            var authorize = typeof(AttendancesController).GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            Assert.NotNull(authorize);
        }

        [Fact]
        public void AttendancesByDateRangeShouldRequireAdminRole()
        {
            // Arrange
            var method = typeof(AttendancesController).GetMethod(nameof(AttendancesController.AttendancesByDateRange));

            // Act
            var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            Assert.NotNull(authorize);
            Assert.Equal("admin", authorize.Roles);
        }

        [Fact]
        public void AttendancesByDateRangeShouldBeHttpGetByDateRange()
        {
            // Arrange
            var method = typeof(AttendancesController).GetMethod(nameof(AttendancesController.AttendancesByDateRange));

            // Act
            var httpGet = method!.GetCustomAttribute<HttpGetAttribute>();

            // Assert
            Assert.NotNull(httpGet);
            Assert.Equal("ByDateRange", httpGet.Template);
        }
    }
}
