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
    public class RefreshSessionCommandHandlerTests
    {
        private const string PresentedToken = "presented-token";
        private const string PresentedHash = "presented-hash";
        private const string NewToken = "new-token";
        private const string NewHash = "new-hash";

        private static readonly DateTimeOffset SignedIn = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset Now = SignedIn.AddDays(1);

        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ISessionRepository> _sessionRepositoryMock;
        private readonly Mock<IAccessTokenIssuer> _accessTokenIssuerMock;
        private readonly Mock<IRefreshTokenGenerator> _refreshTokenGeneratorMock;
        private readonly Mock<TimeProvider> _timeProviderMock;
        private readonly Mock<ILogger<RefreshSessionCommandHandler>> _loggerMock;
        private readonly UserSession _session;
        private readonly User _user = new() { UserName = "jdoe", PasswordHash = "hash", Role = "admin" };

        public RefreshSessionCommandHandlerTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _sessionRepositoryMock = new Mock<ISessionRepository>();
            _accessTokenIssuerMock = new Mock<IAccessTokenIssuer>();
            _refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();
            _timeProviderMock = new Mock<TimeProvider>();
            _loggerMock = new Mock<ILogger<RefreshSessionCommandHandler>>();
            _session = new UserSession { SessionId = Guid.NewGuid(), UserName = "jdoe", CreatedAt = SignedIn, LastRenewedAt = SignedIn };

            _timeProviderMock.Setup(time => time.GetUtcNow()).Returns(Now);
            _refreshTokenGeneratorMock.Setup(generator => generator.Hash(PresentedToken)).Returns(PresentedHash);
            _refreshTokenGeneratorMock.Setup(generator => generator.Generate()).Returns(NewToken);
            _refreshTokenGeneratorMock.Setup(generator => generator.Hash(NewToken)).Returns(NewHash);
            _sessionRepositoryMock.Setup(repo => repo.GetToken(PresentedHash))
                .ReturnsAsync(new RefreshToken { TokenHash = PresentedHash, SessionId = _session.SessionId });
            _sessionRepositoryMock.Setup(repo => repo.GetSession(_session.SessionId)).ReturnsAsync(_session);
            _sessionRepositoryMock.Setup(repo => repo.TrySpendToken(PresentedHash, Now, It.IsAny<TimeSpan>())).ReturnsAsync(true);
            _userRepositoryMock.Setup(repo => repo.GetUserByUserName("jdoe")).ReturnsAsync(_user);
            _accessTokenIssuerMock.Setup(issuer => issuer.Issue(It.IsAny<User>())).Returns((User user) => $"access-for-{user.Role}");
            _accessTokenIssuerMock.SetupGet(issuer => issuer.LifetimeSeconds).Returns(900);
        }

        private RefreshSessionCommandHandler CreateHandler() => new(
            _userRepositoryMock.Object, _sessionRepositoryMock.Object, _accessTokenIssuerMock.Object,
            _refreshTokenGeneratorMock.Object, _timeProviderMock.Object, TestSettings.Create(), _loggerMock.Object);

        private Task<application.Responses.TokenResponse> Refresh(string token = PresentedToken) =>
            CreateHandler().Handle(new RefreshSessionCommand(token), CancellationToken.None);

        private void VerifySessionEnded(string reason) =>
            _sessionRepositoryMock.Verify(repo => repo.EndSession(_session.SessionId, Now, reason, It.IsAny<TimeSpan>()), Times.Once);

        [Fact]
        public async Task HandleShouldReturnNewPairAndSpendPresentedToken()
        {
            // Act
            var result = await Refresh();

            // Assert
            Assert.Equal(NewToken, result.RefreshToken);
            Assert.Equal("access-for-admin", result.AccessToken);
            Assert.Equal(900, result.ExpiresIn);
            _sessionRepositoryMock.Verify(repo => repo.AddToken(
                It.Is<RefreshToken>(token => token.TokenHash == NewHash && token.SessionId == _session.SessionId), Now, TimeSpan.FromDays(29)), Times.Once);
            _sessionRepositoryMock.Verify(repo => repo.TrySpendToken(PresentedHash, Now, TimeSpan.FromDays(29)), Times.Once);
        }

        [Fact]
        public async Task HandleShouldUseCurrentRole()
        {
            // Arrange
            _userRepositoryMock.Setup(repo => repo.GetUserByUserName("jdoe"))
                .ReturnsAsync(new User { UserName = "jdoe", PasswordHash = "hash", Role = "user" });

            // Act
            var result = await Refresh();

            // Assert
            Assert.Equal("access-for-user", result.AccessToken);
        }

        [Fact]
        public async Task HandleShouldRejectUnknownTokenWithoutChanges()
        {
            // Arrange
            _refreshTokenGeneratorMock.Setup(generator => generator.Hash("unknown")).Returns("unknown-hash");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => Refresh("unknown"));
            _sessionRepositoryMock.Verify(repo => repo.GetToken("unknown-hash"), Times.Once);
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task HandleShouldRejectMissingToken(string? token)
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => Refresh(token!));
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task HandleShouldRejectExpiredSession()
        {
            // Arrange
            _session.LastRenewedAt = Now.AddDays(-7);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => Refresh());
            _sessionRepositoryMock.Verify(repo => repo.AddToken(It.IsAny<RefreshToken>(), It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()), Times.Never);
            _sessionRepositoryMock.Verify(repo => repo.TrySpendToken(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task HandleShouldRevokeSessionWhenTokenIsSpent()
        {
            // Arrange
            _sessionRepositoryMock.Setup(repo => repo.GetToken(PresentedHash))
                .ReturnsAsync(new RefreshToken { TokenHash = PresentedHash, SessionId = _session.SessionId, SpentAt = Now.AddMinutes(-20) });

            // Act & Assert
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => Refresh());
            VerifySessionEnded("reuse");
            _sessionRepositoryMock.Verify(repo => repo.AddToken(It.IsAny<RefreshToken>(), It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()), Times.Never);
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(_session.SessionId.ToString()) && !state.ToString()!.Contains(PresentedToken)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task HandleShouldRejectEndedSession()
        {
            // Arrange
            _session.EndedAt = Now.AddHours(-1);
            _session.EndReason = "logout";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => Refresh());
            _sessionRepositoryMock.Verify(repo => repo.EndSession(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Never);
            _sessionRepositoryMock.Verify(repo => repo.AddToken(It.IsAny<RefreshToken>(), It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task HandleShouldRevokeSessionWhenSpendIsNotApplied()
        {
            // Arrange
            _sessionRepositoryMock.Setup(repo => repo.TrySpendToken(PresentedHash, Now, It.IsAny<TimeSpan>())).ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => Refresh());
            VerifySessionEnded("reuse");
        }

        [Fact]
        public async Task HandleShouldEndSessionWhenUserIsMissing()
        {
            // Arrange
            _userRepositoryMock.Setup(repo => repo.GetUserByUserName("jdoe")).ReturnsAsync((User)null!);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => Refresh());
            VerifySessionEnded("account-missing");
        }

        [Fact]
        public async Task HandleShouldNotSpendTokenWhenEarlierStepFails()
        {
            // Arrange
            _sessionRepositoryMock.Setup(repo => repo.AddToken(It.IsAny<RefreshToken>(), It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()))
                .ThrowsAsync(new DataUnavailableException(new Exception("down")));

            // Act & Assert
            await Assert.ThrowsAsync<DataUnavailableException>(() => Refresh());
            _sessionRepositoryMock.Verify(repo => repo.TrySpendToken(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()), Times.Never);
        }
    }
}
