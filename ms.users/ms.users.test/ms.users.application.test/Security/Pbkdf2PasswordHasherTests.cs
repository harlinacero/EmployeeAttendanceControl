using ms.users.application.Security;
using System.Text;

namespace ms.users.application.test.Security
{
    public class Pbkdf2PasswordHasherTests
    {
        private const string Password = "s3cret-Password";

        private readonly Pbkdf2PasswordHasher _hasher = new();

        [Fact]
        public void HashShouldNotContainPassword()
        {
            // Act
            var stored = _hasher.Hash(Password);

            // Assert
            Assert.DoesNotContain(Password, stored);
            Assert.DoesNotContain(Convert.ToBase64String(Encoding.UTF8.GetBytes(Password)), stored);
            Assert.StartsWith("pbkdf2-sha256$600000$", stored);
        }

        [Fact]
        public void HashShouldDifferForSamePassword()
        {
            // Act
            var first = _hasher.Hash(Password);
            var second = _hasher.Hash(Password);

            // Assert
            Assert.NotEqual(first, second);
        }

        [Fact]
        public void VerifyShouldAcceptCorrectPassword()
        {
            // Arrange
            var stored = _hasher.Hash(Password);

            // Act
            var valid = _hasher.Verify(Password, stored);

            // Assert
            Assert.True(valid);
        }

        [Fact]
        public void VerifyShouldRejectWrongPassword()
        {
            // Arrange
            var stored = _hasher.Hash(Password);

            // Act
            var valid = _hasher.Verify("another-password", stored);

            // Assert
            Assert.False(valid);
        }

        [Fact]
        public void VerifyShouldRejectStoredValueUsedAsPassword()
        {
            // Arrange
            var stored = _hasher.Hash(Password);

            // Act
            var valid = _hasher.Verify(stored, stored);

            // Assert
            Assert.False(valid);
        }

        [Fact]
        public void VerifyShouldRejectReadableStoredValue()
        {
            // Act
            var valid = _hasher.Verify(Password, Password);

            // Assert
            Assert.False(valid);
        }

        [Theory]
        [InlineData("1234")]
        [InlineData("pbkdf2-sha256$x")]
        [InlineData("pbkdf2-sha256$600000$not base64$also not")]
        [InlineData("")]
        [InlineData(null)]
        public void IsProtectedShouldRecognizeOnlyCompleteFormat(string? stored)
        {
            // Act
            var isProtected = _hasher.IsProtected(stored!);

            // Assert
            Assert.False(isProtected);
            Assert.True(_hasher.IsProtected(_hasher.Hash(Password)));
        }
    }
}
