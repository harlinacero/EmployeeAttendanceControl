using System.Text.Json.Serialization;

namespace ms.users.application.Responses
{
    public record JwksResponse([property: JsonPropertyName("keys")] IReadOnlyList<JsonWebKeyResponse> Keys);
}
