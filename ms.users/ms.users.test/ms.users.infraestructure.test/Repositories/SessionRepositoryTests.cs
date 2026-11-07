using Cassandra;
using Cassandra.Mapping;
using Moq;
using ms.users.domain.Entities;
using ms.users.domain.Exceptions;
using ms.users.infraestructure.Data;
using ms.users.infraestructure.Repositories;
using System.Net;

namespace ms.users.infraestructure.test.Repositories
{
    public class SessionRepositoryTests
    {
        private static readonly DateTimeOffset Now = new(2026, 11, 7, 10, 0, 0, TimeSpan.Zero);

        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IUsersContext> _usersContextMock;
        private readonly List<Cql> _executed = [];

        public SessionRepositoryTests()
        {
            _mapperMock = new Mock<IMapper>();
            _usersContextMock = new Mock<IUsersContext>();
            _usersContextMock.Setup(context => context.GetMapper()).Returns(_mapperMock.Object);
            _mapperMock.Setup(mapper => mapper.ExecuteAsync(It.IsAny<Cql>()))
                .Callback((Cql cql) => _executed.Add(cql))
                .Returns(Task.CompletedTask);
        }

        private SessionRepository CreateRepository() => new(_usersContextMock.Object);

        [Fact]
        public async Task CreateShouldInsertSessionAndTokenWithTimeToLive()
        {
            // Arrange
            var session = new UserSession { SessionId = Guid.NewGuid(), UserName = "jdoe", CreatedAt = Now, LastRenewedAt = Now };
            var token = new RefreshToken { TokenHash = "hash", SessionId = session.SessionId };

            // Act
            await CreateRepository().CreateSession(session, token, TimeSpan.FromDays(30));

            // Assert
            Assert.Equal(2, _executed.Count);
            Assert.StartsWith("INSERT INTO user_session", _executed[0].Statement);
            Assert.Contains("USING TTL ?", _executed[0].Statement);
            Assert.Equal(new object[] { session.SessionId, "jdoe", Now, Now, 2592000 }, _executed[0].Arguments);
            Assert.StartsWith("INSERT INTO refresh_token", _executed[1].Statement);
            Assert.Equal(new object[] { "hash", session.SessionId, 2592000 }, _executed[1].Arguments);
        }

        [Fact]
        public async Task AddTokenShouldInsertTokenAndRenewSession()
        {
            // Arrange
            var sessionId = Guid.NewGuid();

            // Act
            await CreateRepository().AddToken(new RefreshToken { TokenHash = "new", SessionId = sessionId }, Now, TimeSpan.FromDays(29));

            // Assert
            Assert.Equal(2, _executed.Count);
            Assert.StartsWith("INSERT INTO refresh_token", _executed[0].Statement);
            Assert.Equal("UPDATE user_session USING TTL ? SET session_last_renewed_at = ? WHERE session_id = ?", _executed[1].Statement);
            Assert.Equal(new object[] { 2505600, Now, sessionId }, _executed[1].Arguments);
        }

        [Fact]
        public async Task TrySpendShouldUseConditionalUpdate()
        {
            // Arrange
            Cql? executed = null;
            _mapperMock.Setup(mapper => mapper.UpdateIfAsync<RefreshToken>(It.IsAny<Cql>()))
                .Callback((Cql cql) => executed = cql)
                .ReturnsAsync(new AppliedInfo<RefreshToken>(false));

            // Act
            var applied = await CreateRepository().TrySpendToken("hash", Now, TimeSpan.FromDays(29));

            // Assert
            Assert.False(applied);
            Assert.NotNull(executed);
            Assert.Equal("USING TTL ? SET token_spent_at = ? WHERE token_hash = ? IF token_spent_at = null", executed.Statement);
            Assert.Equal(new object[] { 2505600, Now, "hash" }, executed.Arguments);
        }

        [Fact]
        public async Task EndShouldSetEndedAtAndReason()
        {
            // Arrange
            var sessionId = Guid.NewGuid();

            // Act
            await CreateRepository().EndSession(sessionId, Now, "reuse", TimeSpan.FromDays(1));

            // Assert
            var cql = Assert.Single(_executed);
            Assert.Equal("UPDATE user_session USING TTL ? SET session_ended_at = ?, session_end_reason = ? WHERE session_id = ?", cql.Statement);
            Assert.Equal(new object[] { 86400, Now, "reuse", sessionId }, cql.Arguments);
        }

        [Fact]
        public async Task GetTokenShouldReturnNullWhenNotFound()
        {
            // Arrange
            _mapperMock.Setup(mapper => mapper.FirstOrDefaultAsync<RefreshToken>(It.IsAny<string>(), It.IsAny<object[]>()))
                .ReturnsAsync((RefreshToken)null!);

            // Act
            var token = await CreateRepository().GetToken("unknown");

            // Assert
            Assert.Null(token);
        }

        [Fact]
        public async Task GetSessionShouldQueryBySessionId()
        {
            // Arrange
            var session = new UserSession { SessionId = Guid.NewGuid(), UserName = "jdoe" };
            _mapperMock.Setup(mapper => mapper.FirstOrDefaultAsync<UserSession>(
                    "SELECT * FROM user_session WHERE session_id = ?", It.Is<object[]>(args => (Guid)args[0] == session.SessionId)))
                .ReturnsAsync(session);

            // Act
            var result = await CreateRepository().GetSession(session.SessionId);

            // Assert
            Assert.Same(session, result);
        }

        [Fact]
        public async Task ShouldThrowDataUnavailableWhenDriverFails()
        {
            // Arrange
            _mapperMock.Setup(mapper => mapper.ExecuteAsync(It.IsAny<Cql>()))
                .ThrowsAsync(new NoHostAvailableException(new Dictionary<IPEndPoint, Exception>()));

            // Act & Assert
            await Assert.ThrowsAsync<DataUnavailableException>(() =>
                CreateRepository().AddToken(new RefreshToken { TokenHash = "new", SessionId = Guid.NewGuid() }, Now, TimeSpan.FromDays(1)));
        }
    }
}
