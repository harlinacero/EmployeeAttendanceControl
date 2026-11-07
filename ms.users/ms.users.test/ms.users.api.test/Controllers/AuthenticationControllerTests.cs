using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ms.users.api.Controllers;
using ms.users.application.Commands;
using ms.users.application.Request;
using ms.users.application.Responses;
using ms.users.domain.Exceptions;

namespace ms.users.api.test.Controllers
{
    public class AuthenticationControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly TokenResponse _tokens = new("access", "refresh", 900);

        public AuthenticationControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
        }

        private AuthenticationController CreateController() => new(_mediatorMock.Object);

        [Fact]
        public async Task DoLoginShouldReturnOkWithTokens()
        {
            // Arrange
            _mediatorMock.Setup(m => m.Send(new LoginCommand("testuser", "testpassword"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_tokens);

            // Act
            var result = await CreateController().DoLogin(new LoginCredentialsRequest { UserName = "testuser", Password = "testpassword" });

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(_tokens, okResult.Value);
        }

        [Fact]
        public async Task DoLoginShouldReturnUnauthorizedForInvalidCredentials()
        {
            // Arrange
            _mediatorMock.Setup(m => m.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidCredentialsException());

            // Act
            var result = await CreateController().DoLogin(new LoginCredentialsRequest { UserName = "testuser", Password = "wrong" });

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal(AuthenticationController.InvalidCredentialsMessage, unauthorized.Value);
        }

        [Fact]
        public async Task DoLoginShouldNotCatchDataUnavailable()
        {
            // Arrange
            _mediatorMock.Setup(m => m.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new DataUnavailableException(new Exception("down")));

            // Act & Assert
            await Assert.ThrowsAsync<DataUnavailableException>(() =>
                CreateController().DoLogin(new LoginCredentialsRequest { UserName = "testuser", Password = "testpassword" }));
        }

        [Fact]
        public async Task RefreshShouldReturnOkWithTokens()
        {
            // Arrange
            _mediatorMock.Setup(m => m.Send(new RefreshSessionCommand("refresh"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_tokens);

            // Act
            var result = await CreateController().Refresh(new RefreshTokenRequest { RefreshToken = "refresh" });

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(_tokens, okResult.Value);
        }

        [Fact]
        public async Task RefreshShouldReturnUnauthorizedForInvalidCredentials()
        {
            // Arrange
            _mediatorMock.Setup(m => m.Send(It.IsAny<RefreshSessionCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidCredentialsException());

            // Act
            var result = await CreateController().Refresh(new RefreshTokenRequest { RefreshToken = "spent" });

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task LogoutShouldReturnNoContent()
        {
            // Act
            var result = await CreateController().Logout(new LogoutRequest { RefreshToken = "refresh" });

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mediatorMock.Verify(m => m.Send(new LogoutCommand("refresh"), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
