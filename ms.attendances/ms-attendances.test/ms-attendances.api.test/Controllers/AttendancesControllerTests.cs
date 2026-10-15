using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;

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
    }
}
