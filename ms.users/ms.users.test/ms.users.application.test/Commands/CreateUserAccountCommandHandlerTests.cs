using Moq;
using Microsoft.Extensions.Logging;

using ms.users.domain.Interfaces;
using ms.users.application.Commands.Handlers;
using ms.users.application.Commands;
using ms.users.application.Security;
using ms.users.domain.Entities;

namespace ms.users.application.test.Commands
{
    public class CreateUserAccountCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<ILogger<CreateUserAccountCommandHandler>> _loggerMock;

        public CreateUserAccountCommandHandlerTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _loggerMock = new Mock<ILogger<CreateUserAccountCommandHandler>>();
        }

        [Fact]
        public async Task HandleShouldCreateUserAccount()
        {
            // Arrange
            var command = new CreateUserAccountCommand("testuser", "password", "user");
            _userRepositoryMock.Setup(repo => repo.CreateUser(It.IsAny<User>()))
                .ReturnsAsync((User user) => user);

            var handler = new CreateUserAccountCommandHandler(_userRepositoryMock.Object, _passwordHasherMock.Object, _loggerMock.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(command.UserName, result);
            _userRepositoryMock.Verify(repo => repo.CreateUser(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task HandleShouldStoreOnlyHashedPassword()
        {
            // Arrange
            var command = new CreateUserAccountCommand("testuser", "password", "user");
            _passwordHasherMock.Setup(hasher => hasher.Hash("password")).Returns("pbkdf2-sha256$hashed");
            _userRepositoryMock.Setup(repo => repo.CreateUser(It.IsAny<User>()))
                .ReturnsAsync((User user) => user);

            var handler = new CreateUserAccountCommandHandler(_userRepositoryMock.Object, _passwordHasherMock.Object, _loggerMock.Object);

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            _userRepositoryMock.Verify(repo => repo.CreateUser(It.Is<User>(user =>
                user.UserName == "testuser" && user.PasswordHash == "pbkdf2-sha256$hashed" && user.Role == "user")), Times.Once);
        }
    }
}
