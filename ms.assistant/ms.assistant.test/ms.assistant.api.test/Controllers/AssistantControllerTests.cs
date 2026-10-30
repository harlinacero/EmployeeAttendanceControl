using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ms.assistant.api.Controllers;
using ms.assistant.application.Commands;
using ms.assistant.application.Requests;
using ms.assistant.application.Responses;
using System.Reflection;
using System.Security.Claims;

namespace ms.assistant.api.test.Controllers
{
    public class AssistantControllerTests
    {
        private readonly Mock<IMediator> _mediator;

        public AssistantControllerTests()
        {
            _mediator = new Mock<IMediator>();
            _mediator.Setup(mediator => mediator.Send(It.IsAny<ChatCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatResponse { Answer = "Hola" });
        }

        private AssistantController CreateController(string userName, string? role)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userName) };
            if (role is not null)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer")) };
            httpContext.Request.Headers.Authorization = "Bearer abc";
            return new AssistantController(_mediator.Object) { ControllerContext = new ControllerContext { HttpContext = httpContext } };
        }

        private static ChatRequest Request() => new()
        {
            Question = "¿Cuántas veces falté?",
            History = [new ChatMessageRequest { Role = "user", Content = "hola" }]
        };

        [Fact]
        public async Task ChatShouldReturnOkWithChatResponse()
        {
            // Arrange
            var controller = CreateController("jdoe", null);

            // Act
            var result = await controller.Chat(Request());

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<ChatResponse>(ok.Value);
            Assert.Equal("Hola", response.Answer);
        }

        [Fact]
        public async Task ChatShouldSendCommandWithCallerIdentityRoleAndToken()
        {
            // Arrange
            var controller = CreateController("jdoe", "admin");
            var request = Request();

            // Act
            await controller.Chat(request);

            // Assert
            _mediator.Verify(mediator => mediator.Send(It.Is<ChatCommand>(command =>
                command.UserName == "jdoe"
                && command.IsAdmin
                && command.Token == "Bearer abc"
                && command.Question == request.Question
                && command.History.SequenceEqual(request.History)), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ChatShouldSendIsAdminFalseForRegularUser()
        {
            // Arrange
            var controller = CreateController("jdoe", "user");

            // Act
            await controller.Chat(Request());

            // Assert
            _mediator.Verify(mediator => mediator.Send(It.Is<ChatCommand>(command => !command.IsAdmin), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void ControllerShouldRequireAuthentication()
        {
            // Act
            var authorize = typeof(AssistantController).GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            Assert.NotNull(authorize);
            Assert.Null(authorize.Roles);
        }
    }
}
