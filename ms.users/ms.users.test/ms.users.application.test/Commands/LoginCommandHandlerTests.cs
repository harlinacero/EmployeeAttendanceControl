using Moq;
using ms.users.application.Commands;
using ms.users.application.Commands.Handlers;
using ms.users.application.Security;
using ms.users.domain.Entities;
using ms.users.domain.Exceptions;
using ms.users.domain.Interfaces;

namespace ms.users.application.test.Commands
{
    public class LoginCommandHandlerTests
    {
        private static readonly DateTimeOffset Now = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);

        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ISessionRepository> _sessionRepositoryMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IAccessTokenIssuer> _accessTokenIssuerMock;
        private readonly Mock<IRefreshTokenGenerator> _refreshTokenGeneratorMock;
        private readonly Mock<TimeProvider> _timeProviderMock;
        private readonly User _user = new() { UserName = "jdoe", PasswordHash = "stored-hash", Role = "user" };

        public LoginCommandHandlerTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _sessionRepositoryMock = new Mock<ISessionRepository>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _accessTokenIssuerMock = new Mock<IAccessTokenIssuer>();
            _refreshTokenGeneratorMock = new Mock<IRefreshTokenGenerator>();
            _timeProviderMock = new Mock<TimeProvider>();

            _timeProviderMock.Setup(time => time.GetUtcNow()).Returns(Now);
            _userRepositoryMock.Setup(repo => repo.GetUserByUserName("jdoe")).ReturnsAsync(_user);
            _passwordHasherMock.Setup(hasher => hasher.Verify("right", "stored-hash")).Returns(true);
            _accessTokenIssuerMock.Setup(issuer => issuer.Issue(_user)).Returns("access-token");
            _accessTokenIssuerMock.SetupGet(issuer => issuer.LifetimeSeconds).Returns(900);
            _refreshTokenGeneratorMock.Setup(generator => generator.Generate()).Returns("refresh-token");
            _refreshTokenGeneratorMock.Setup(generator => generator.Hash("refresh-token")).Returns("refresh-hash");
        }

        private LoginCommandHandler CreateHandler() => new(
            _userRepositoryMock.Object, _sessionRepositoryMock.Object, _passwordHasherMock.Object,
            _accessTokenIssuerMock.Object, _refreshTokenGeneratorMock.Object, _timeProviderMock.Object, TestSettings.Create());

        [Fact]
        public async Task HandleShouldReturnTokensAndCreateSession()
        {
            // Act
            var result = await CreateHandler().Handle(new LoginCommand("jdoe", "right"), CancellationToken.None);

            // Assert
            Assert.Equal("access-token", result.AccessToken);
            Assert.Equal("refresh-token", result.RefreshToken);
            Assert.Equal(900, result.ExpiresIn);
            _sessionRepositoryMock.Verify(repo => repo.CreateSession(
                It.Is<UserSession>(session => session.UserName == "jdoe" && session.CreatedAt == Now && session.LastRenewedAt == Now && session.EndedAt == null),
                It.IsAny<RefreshToken>(),
                TimeSpan.FromDays(30)), Times.Once);
        }

        [Fact]
        public async Task HandleShouldStoreOnlyRefreshTokenHash()
        {
            // Act
            await CreateHandler().Handle(new LoginCommand("jdoe", "right"), CancellationToken.None);

            // Assert
            _sessionRepositoryMock.Verify(repo => repo.CreateSession(
                It.IsAny<UserSession>(),
                It.Is<RefreshToken>(token => token.TokenHash == "refresh-hash" && token.SpentAt == null),
                It.IsAny<TimeSpan>()), Times.Once);
        }

        [Fact]
        public async Task HandleShouldCreateIndependentSessionEachTime()
        {
            // Arrange
            var sessionIds = new List<Guid>();
            _sessionRepositoryMock.Setup(repo => repo.CreateSession(It.IsAny<UserSession>(), It.IsAny<RefreshToken>(), It.IsAny<TimeSpan>()))
                .Callback((UserSession session, RefreshToken token, TimeSpan _) =>
                {
                    Assert.Equal(session.SessionId, token.SessionId);
                    sessionIds.Add(session.SessionId);
                });
            var handler = CreateHandler();

            // Act
            await handler.Handle(new LoginCommand("jdoe", "right"), CancellationToken.None);
            await handler.Handle(new LoginCommand("jdoe", "right"), CancellationToken.None);

            // Assert
            Assert.Equal(2, sessionIds.Distinct().Count());
        }

        [Fact]
        public async Task HandleShouldRejectWrongPassword()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => CreateHandler().Handle(new LoginCommand("jdoe", "wrong"), CancellationToken.None));
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task HandleShouldRejectUnknownUserAfterVerifyingDummyHash()
        {
            // Arrange
            _userRepositoryMock.Setup(repo => repo.GetUserByUserName("ghost")).ReturnsAsync((User)null!);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => CreateHandler().Handle(new LoginCommand("ghost", "any"), CancellationToken.None));
            _passwordHasherMock.Verify(hasher => hasher.Verify("any", It.Is<string>(stored => stored.StartsWith("pbkdf2-sha256$"))), Times.Once);
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null, "right")]
        [InlineData("jdoe", null)]
        [InlineData("", "")]
        public async Task HandleShouldRejectMissingData(string? userName, string? password)
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidCredentialsException>(() => CreateHandler().Handle(new LoginCommand(userName!, password!), CancellationToken.None));
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }
    }
}
