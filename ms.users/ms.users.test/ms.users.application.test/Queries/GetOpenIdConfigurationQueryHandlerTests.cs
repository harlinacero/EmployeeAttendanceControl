using ms.users.application.Queries;
using ms.users.application.Queries.Handlers;
using System.Text.Json;

namespace ms.users.application.test.Queries
{
    public class GetOpenIdConfigurationQueryHandlerTests
    {
        [Fact]
        public async Task HandleShouldReturnIssuerAndJwksUri()
        {
            // Arrange
            var handler = new GetOpenIdConfigurationQueryHandler(TestSettings.Create());

            // Act
            var result = await handler.Handle(new GetOpenIdConfigurationQuery("http://ms.users.api/.well-known/jwks.json"), CancellationToken.None);

            // Assert
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(result));
            Assert.Equal(TestSettings.Issuer, document.RootElement.GetProperty("issuer").GetString());
            Assert.Equal("http://ms.users.api/.well-known/jwks.json", document.RootElement.GetProperty("jwks_uri").GetString());
        }
    }
}
