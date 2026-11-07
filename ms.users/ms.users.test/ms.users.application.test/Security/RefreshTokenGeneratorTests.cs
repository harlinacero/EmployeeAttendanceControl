using Microsoft.IdentityModel.Tokens;
using ms.users.application.Security;

namespace ms.users.application.test.Security
{
    public class RefreshTokenGeneratorTests
    {
        private readonly RefreshTokenGenerator _generator = new();

        [Fact]
        public void GenerateShouldReturnUrlSafe256BitValue()
        {
            // Act
            var token = _generator.Generate();

            // Assert
            Assert.Equal(32, Base64UrlEncoder.DecodeBytes(token).Length);
            Assert.DoesNotContain('+', token);
            Assert.DoesNotContain('/', token);
            Assert.DoesNotContain('=', token);
        }

        [Fact]
        public void GenerateShouldNotRepeat()
        {
            // Act
            var tokens = Enumerable.Range(0, 100).Select(_ => _generator.Generate()).ToList();

            // Assert
            Assert.Equal(tokens.Count, tokens.Distinct().Count());
        }

        [Fact]
        public void HashShouldBeStableAndNotContainToken()
        {
            // Arrange
            var token = _generator.Generate();

            // Act
            var first = _generator.Hash(token);
            var second = _generator.Hash(token);

            // Assert
            Assert.Equal(first, second);
            Assert.Equal(64, first.Length);
            Assert.DoesNotContain(token, first);
            Assert.NotEqual(first, _generator.Hash(_generator.Generate()));
        }
    }
}
