using Moq;
using ms.users.application.Commands;
using ms.users.application.Commands.Handlers;
using ms.users.application.Security;
using ms.users.domain.Entities;
using ms.users.domain.Exceptions;
using ms.users.domain.Interfaces;

namespace ms.users.application.test.Commands
{
    public class LogoutCommandHandlerTests
    {
        private static readonly DateTimeOffset Now = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);

        private readonly Mock<ISessionRepository> _sessionRepositoryMock;
        private readonly Mock<IRefreshTokenGenerator> _refreshTokenGeneratorMock;
        private readonly Mock<TimeProvider> _timeProviderMock;
        private readonly UserSession _session;

        public LogoutCommandHandlerTests()
        {
            _sessionRepositoryMock = new Mock<ISessionRepository>();
            _refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();
            _timeProviderMock = new Mock<TimeProvider>();
            _session = new UserSession { SessionId = Guid.NewGuid(), UserName = "jdoe", CreatedAt = Now.AddDays(-1), LastRenewedAt = Now.AddHours(-1) };

            _timeProviderMock.Setup(time => time.GetUtcNow()).Returns(Now);
            _refreshTokenGeneratorMock.Setup(generator => generator.Hash("token")).Returns("hash");
            _sessionRepositoryMock.Setup(repo => repo.GetToken("hash"))
                .ReturnsAsync(new RefreshToken { TokenHash = "hash", SessionId = _session.SessionId });
            _sessionRepositoryMock.Setup(repo => repo.GetSession(_session.SessionId)).ReturnsAsync(_session);
        }

        private Task Logout(string token = "token") =>
            new LogoutCommandHandler(_sessionRepositoryMock.Object, _refreshTokenGeneratorMock.Object, _timeProviderMock.Object, TestSettings.Create())
                .Handle(new LogoutCommand(token), CancellationToken.None);

        [Fact]
        public async Task HandleShouldEndSessionOfPresentedToken()
        {
            // Act
            await Logout();

            // Assert
            _sessionRepositoryMock.Verify(repo => repo.EndSession(_session.SessionId, Now, "logout", TimeSpan.FromDays(29)), Times.Once);
        }

        [Fact]
        public async Task HandleShouldDoNothingForUnknownToken()
        {
            // Arrange
            _refreshTokenGeneratorMock.Setup(generator => generator.Hash("unknown")).Returns("unknown-hash");

            // Act
            await Logout("unknown");

            // Assert
            _sessionRepositoryMock.Verify(repo => repo.EndSession(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task HandleShouldDoNothingForEndedSession()
        {
            // Arrange
            _session.EndedAt = Now.AddMinutes(-5);

            // Act
            await Logout();

            // Assert
            _sessionRepositoryMock.Verify(repo => repo.EndSession(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task HandleShouldEndSessionWhenTokenIsSpent()
        {
            // Arrange
            _sessionRepositoryMock.Setup(repo => repo.GetToken("hash"))
                .ReturnsAsync(new RefreshToken { TokenHash = "hash", SessionId = _session.SessionId, SpentAt = Now.AddMinutes(-30) });

            // Act
            await Logout();

            // Assert
            _sessionRepositoryMock.Verify(repo => repo.EndSession(_session.SessionId, Now, "logout", It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task HandleShouldNotCatchDataUnavailable()
        {
            // Arrange
            _sessionRepositoryMock.Setup(repo => repo.GetToken("hash")).ThrowsAsync(new DataUnavailableException(new Exception("down")));

            // Act & Assert
            await Assert.ThrowsAsync<DataUnavailableException>(() => Logout());
        }
    }
}
