using Moq;
using Microsoft.Extensions.Logging;

using ms.users.domain.Interfaces;
using ms.users.application.Commands.Handlers;
using ms.users.application.Commands;
using ms.users.domain.Entities;

namespace ms.users.application.test.Commands
{
    public class CreateUserAccountCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ILogger<CreateUserAccountCommandHandler>> _loggerMock;

        public CreateUserAccountCommandHandlerTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _loggerMock = new Mock<ILogger<CreateUserAccountCommandHandler>>();
        }

        [Fact]
        public async Task HandleShouldCreateUserAccount()
        {
            // Arrange
            var command = new CreateUserAccountCommand("testuser", "password", "user");

            var user = new User
            {
                UserName = command.UserName,
                Password = command.Password,
                Role = command.Role
            };
            _userRepositoryMock.Setup(repo => repo.CreateUser(It.IsAny<User>()))
                .ReturnsAsync(user);

            var handler = new CreateUserAccountCommandHandler(_userRepositoryMock.Object, _loggerMock.Object);
            
            // Act
            var result = await handler.Handle(command, CancellationToken.None);
            
            // Assert
            Assert.Equal(command.UserName, result);
            _userRepositoryMock.Verify(repo => repo.CreateUser(It.IsAny<User>()), Times.Once);
        }
    }
}
