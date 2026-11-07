using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ms.users.api.Controllers;
using ms.users.application.Queries;
using ms.users.application.Responses;

namespace ms.users.api.test.Controllers
{
    public class WellKnownControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;

        public WellKnownControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
        }

        [Fact]
        public async Task OpenIdConfigurationShouldReturnOk()
        {
            // Arrange
            var configuration = new OpenIdConfigurationResponse("ms.users", "http://ms.users.api/.well-known/jwks.json");
            _mediatorMock.Setup(m => m.Send(new GetOpenIdConfigurationQuery("http://ms.users.api/.well-known/jwks.json"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(configuration);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "http";
            httpContext.Request.Host = new HostString("ms.users.api");
            var controller = new WellKnownController(_mediatorMock.Object) { ControllerContext = new ControllerContext { HttpContext = httpContext } };

            // Act
            var result = await controller.OpenIdConfiguration();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(configuration, okResult.Value);
        }

        [Fact]
        public async Task JwksShouldReturnOk()
        {
            // Arrange
            var keys = new JwksResponse([]);
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetJwksQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(keys);
            var controller = new WellKnownController(_mediatorMock.Object);

            // Act
            var result = await controller.Jwks();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(keys, okResult.Value);
        }
    }
}
