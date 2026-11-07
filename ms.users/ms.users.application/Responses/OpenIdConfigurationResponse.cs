using System.Text.Json.Serialization;

namespace ms.users.application.Responses
{
    /// <summary>
    /// The part of the OpenID discovery document that services need to validate tokens.
    /// </summary>
    public record OpenIdConfigurationResponse(
        [property: JsonPropertyName("issuer")] string Issuer,
        [property: JsonPropertyName("jwks_uri")] string JwksUri);
}
