using MediatR;
using Moq;
using ms.users.api.Controllers;
using ms.users.application.Queries;
using ms.users.application.Request;
using System;
using System.Collections.Generic;
using System.Text;

namespace ms.users.api.test.Controllers
{
    public class AuthenticationControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;

        public AuthenticationControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
        }

        [Fact]
        public async Task SendGetUserTokenQueryWhenDoLoginIsCalled()
        {
            // Arrange
            var expectedToken = "some-token";
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserTokenQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedToken);

            var controller = new AuthenticationController(_mediatorMock.Object);

            //act
            var result = await controller.DoLogin(new LoginCredentialsRequest
            {
                UserName = "testuser",
                Password = "testpassword"
            });

            // Assert
            var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);
            var token = Assert.IsType<string>(okResult.Value);
            Assert.Equal(expectedToken, token);
        }

        [Fact]
        public async Task SendUserTokenQueryReturnUnAuthorizedWhenCredentialsAreIncorrect()
        {
            // Arrange
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserTokenQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(It.IsAny<Exception>());

            var controller = new AuthenticationController(_mediatorMock.Object);

            //act
            var result = await controller.DoLogin(new LoginCredentialsRequest
            {
                UserName = "testuser",
                Password = "testpassword"
            });

            // Assert
            var unauthorizedObjectResult = Assert.IsType<Microsoft.AspNetCore.Mvc.UnauthorizedObjectResult>(result);
            Assert.Equal(401, unauthorizedObjectResult.StatusCode);
        }
    }
}
