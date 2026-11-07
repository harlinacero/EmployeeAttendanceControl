using ms.users.domain.Entities;

namespace ms.users.application.test.Entities
{
    public class UserSessionTests
    {
        private static readonly DateTimeOffset SignedIn = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);
        private static readonly TimeSpan Idle = TimeSpan.FromDays(7);
        private static readonly TimeSpan Absolute = TimeSpan.FromDays(30);

        private static UserSession Session(DateTimeOffset lastRenewed, DateTimeOffset? ended = null) => new()
        {
            SessionId = Guid.NewGuid(),
            UserName = "jdoe",
            CreatedAt = SignedIn,
            LastRenewedAt = lastRenewed,
            EndedAt = ended
        };

        [Fact]
        public void IsActiveShouldBeTrueWithinLimits()
        {
            // Arrange
            var session = Session(SignedIn.AddDays(2));

            // Act
            var active = session.IsActive(SignedIn.AddDays(8), Idle, Absolute);

            // Assert
            Assert.True(active);
        }

        [Fact]
        public void IsActiveShouldBeFalseAfterIdleLimit()
        {
            // Arrange
            var session = Session(SignedIn.AddDays(2));

            // Act
            var active = session.IsActive(SignedIn.AddDays(9), Idle, Absolute);

            // Assert
            Assert.False(active);
        }

        [Fact]
        public void IsActiveShouldBeFalseAfterAbsoluteLimitEvenIfRenewedRecently()
        {
            // Arrange
            var session = Session(SignedIn.AddDays(29));

            // Act
            var active = session.IsActive(SignedIn.AddDays(30), Idle, Absolute);

            // Assert
            Assert.False(active);
        }

        [Fact]
        public void IsActiveShouldBeFalseWhenEnded()
        {
            // Arrange
            var session = Session(SignedIn.AddHours(1), ended: SignedIn.AddHours(2));

            // Act
            var active = session.IsActive(SignedIn.AddHours(3), Idle, Absolute);

            // Assert
            Assert.False(active);
        }
    }
}
