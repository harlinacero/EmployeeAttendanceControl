using MediatR;
using Microsoft.IdentityModel.Tokens;
using ms.users.application.Responses;
using ms.users.application.Security;

namespace ms.users.application.Queries.Handlers
{
    public class GetJwksQueryHandler : IRequestHandler<GetJwksQuery, JwksResponse>
    {
        private readonly ISigningKeyProvider _signingKeyProvider;

        public GetJwksQueryHandler(ISigningKeyProvider signingKeyProvider) => _signingKeyProvider = signingKeyProvider;

        public Task<JwksResponse> Handle(GetJwksQuery request, CancellationToken cancellationToken)
        {
            var key = _signingKeyProvider.Key;
            var publicParameters = key.Rsa.ExportParameters(includePrivateParameters: false);
            var jwk = new JsonWebKeyResponse(
                JsonWebAlgorithmsKeyTypes.RSA,
                JsonWebKeyUseNames.Sig,
                SecurityAlgorithms.RsaSha256,
                key.KeyId,
                Base64UrlEncoder.Encode(publicParameters.Modulus),
                Base64UrlEncoder.Encode(publicParameters.Exponent));

            return Task.FromResult(new JwksResponse([jwk]));
        }
    }
}
