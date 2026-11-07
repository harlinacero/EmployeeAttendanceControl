using Microsoft.Extensions.Logging;
using Moq;
using ms.users.application.Commands;
using ms.users.application.Commands.Handlers;
using ms.users.application.Security;
using ms.users.domain.Entities;
using ms.users.domain.Exceptions;
using ms.users.domain.Interfaces;

namespace ms.users.application.test.Commands
{
    public class ProtectStoredPasswordsCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<ILogger<ProtectStoredPasswordsCommandHandler>> _loggerMock;

        public ProtectStoredPasswordsCommandHandlerTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _loggerMock = new Mock<ILogger<ProtectStoredPasswordsCommandHandler>>();

            _userRepositoryMock.Setup(repo => repo.GetAllUsers()).ReturnsAsync(
            [
                new() { UserName = "readable", PasswordHash = "1234", Role = "admin" },
                new() { UserName = "protected", PasswordHash = "pbkdf2-sha256$done", Role = "user" }
            ]);
            _passwordHasherMock.Setup(hasher => hasher.IsProtected("1234")).Returns(false);
            _passwordHasherMock.Setup(hasher => hasher.IsProtected("pbkdf2-sha256$done")).Returns(true);
            _passwordHasherMock.Setup(hasher => hasher.Hash("1234")).Returns("pbkdf2-sha256$new");
            _userRepositoryMock.Setup(repo => repo.ProtectPassword(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        }

        private Task<int> Protect() =>
            new ProtectStoredPasswordsCommandHandler(_userRepositoryMock.Object, _passwordHasherMock.Object, _loggerMock.Object)
                .Handle(new ProtectStoredPasswordsCommand(), CancellationToken.None);

        private void VerifyNoLogContains(string value) =>
            _loggerMock.Verify(logger => logger.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(value)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);

        [Fact]
        public async Task HandleShouldProtectReadablePasswords()
        {
            // Act
            var protectedCount = await Protect();

            // Assert
            Assert.Equal(1, protectedCount);
            _userRepositoryMock.Verify(repo => repo.ProtectPassword("readable", "1234", "pbkdf2-sha256$new"), Times.Once);
        }

        [Fact]
        public async Task HandleShouldSkipProtectedPasswords()
        {
            // Act
            await Protect();

            // Assert
            _userRepositoryMock.Verify(repo => repo.ProtectPassword("protected", It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task HandleShouldPropagateFailureWithoutLoggingValues()
        {
            // Arrange
            _userRepositoryMock.Setup(repo => repo.ProtectPassword("readable", "1234", "pbkdf2-sha256$new"))
                .ThrowsAsync(new DataUnavailableException(new Exception("down")));

            // Act & Assert
            await Assert.ThrowsAsync<DataUnavailableException>(Protect);
            VerifyNoLogContains("1234");
            VerifyNoLogContains("pbkdf2-sha256$");
        }

        [Fact]
        public async Task HandleShouldLogOnlyCounts()
        {
            // Act
            await Protect();

            // Assert
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains('1')),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
            VerifyNoLogContains("1234");
            VerifyNoLogContains("pbkdf2-sha256$");
        }
    }
}
