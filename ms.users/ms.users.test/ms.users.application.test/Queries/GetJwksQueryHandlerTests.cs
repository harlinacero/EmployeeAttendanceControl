using Microsoft.IdentityModel.Tokens;
using Moq;
using ms.users.application.Queries;
using ms.users.application.Queries.Handlers;
using ms.users.application.Security;
using System.Security.Cryptography;
using System.Text.Json;

namespace ms.users.application.test.Queries
{
    public class GetJwksQueryHandlerTests
    {
        private readonly RSA _rsa = RSA.Create(2048);
        private readonly Mock<ISigningKeyProvider> _keysMock;

        public GetJwksQueryHandlerTests()
        {
            _keysMock = new Mock<ISigningKeyProvider>();
            _keysMock.SetupGet(keys => keys.Key).Returns(new RsaSecurityKey(_rsa) { KeyId = "kid-1" });
        }

        [Fact]
        public async Task HandleShouldPublishOnlyPublicParameters()
        {
            // Act
            var result = await new GetJwksQueryHandler(_keysMock.Object).Handle(new GetJwksQuery(), CancellationToken.None);

            // Assert
            var json = JsonSerializer.Serialize(result);
            using var document = JsonDocument.Parse(json);
            var key = Assert.Single(document.RootElement.GetProperty("keys").EnumerateArray());
            var names = key.EnumerateObject().Select(property => property.Name).OrderBy(name => name);
            Assert.Equal(["alg", "e", "kid", "kty", "n", "use"], names);
            var parameters = _rsa.ExportParameters(false);
            Assert.Equal(Base64UrlEncoder.Encode(parameters.Modulus), key.GetProperty("n").GetString());
            Assert.Equal("RSA", key.GetProperty("kty").GetString());
            Assert.Equal("RS256", key.GetProperty("alg").GetString());
        }

        [Fact]
        public async Task HandleShouldUseProviderKeyId()
        {
            // Act
            var result = await new GetJwksQueryHandler(_keysMock.Object).Handle(new GetJwksQuery(), CancellationToken.None);

            // Assert
            Assert.Equal("kid-1", Assert.Single(result.Keys).Kid);
        }
    }
}
