using System.Text.Json.Serialization;

namespace ms.users.application.Responses
{
    /// <summary>
    /// Public RSA key in JWK format (RFC 7517). Has no private parameter by construction.
    /// </summary>
    public record JsonWebKeyResponse(
        [property: JsonPropertyName("kty")] string Kty,
        [property: JsonPropertyName("use")] string Use,
        [property: JsonPropertyName("alg")] string Alg,
        [property: JsonPropertyName("kid")] string Kid,
        [property: JsonPropertyName("n")] string N,
        [property: JsonPropertyName("e")] string E);
}
