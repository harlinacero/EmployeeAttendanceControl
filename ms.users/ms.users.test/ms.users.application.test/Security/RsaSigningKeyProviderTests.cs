using Microsoft.Extensions.Logging;
using Moq;
using ms.users.application.Security;

namespace ms.users.application.test.Security
{
    public class RsaSigningKeyProviderTests : IDisposable
    {
        private readonly string _folder;
        private readonly string _keyPath;
        private readonly Mock<ILogger<RsaSigningKeyProvider>> _loggerMock;

        public RsaSigningKeyProviderTests()
        {
            _folder = Directory.CreateTempSubdirectory("ms.users.keys-").FullName;
            _keyPath = Path.Combine(_folder, "nested", "jwt-signing.pem");
            _loggerMock = new Mock<ILogger<RsaSigningKeyProvider>>();
        }

        public void Dispose()
        {
            Directory.Delete(_folder, recursive: true);
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void ShouldGenerateKeyWhenFileIsMissing()
        {
            // Act
            var provider = new RsaSigningKeyProvider(TestSettings.Create(_keyPath), _loggerMock.Object);

            // Assert
            Assert.True(File.Exists(_keyPath));
            Assert.False(string.IsNullOrEmpty(provider.Key.KeyId));
            var privateKey = File.ReadAllText(_keyPath);
            _loggerMock.Verify(logger => logger.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(provider.Key.KeyId) && !state.ToString()!.Contains("PRIVATE KEY")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
            Assert.Contains("PRIVATE KEY", privateKey);
        }

        [Fact]
        public void ShouldReuseExistingKey()
        {
            // Arrange
            var first = new RsaSigningKeyProvider(TestSettings.Create(_keyPath), _loggerMock.Object);

            // Act
            var second = new RsaSigningKeyProvider(TestSettings.Create(_keyPath), _loggerMock.Object);

            // Assert
            Assert.Equal(first.Key.KeyId, second.Key.KeyId);
        }

        [Fact]
        public void ShouldUseNewKeyWhenFileIsReplaced()
        {
            // Arrange
            var first = new RsaSigningKeyProvider(TestSettings.Create(_keyPath), _loggerMock.Object);
            File.Delete(_keyPath);

            // Act
            var second = new RsaSigningKeyProvider(TestSettings.Create(_keyPath), _loggerMock.Object);

            // Assert
            Assert.NotEqual(first.Key.KeyId, second.Key.KeyId);
        }
    }
}
