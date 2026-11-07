using MediatR;
using Microsoft.Extensions.Options;
using ms.users.application.Extensions;
using ms.users.application.Responses;

namespace ms.users.application.Queries.Handlers
{
    public class GetOpenIdConfigurationQueryHandler : IRequestHandler<GetOpenIdConfigurationQuery, OpenIdConfigurationResponse>
    {
        private readonly string _issuer;

        public GetOpenIdConfigurationQueryHandler(IOptions<SettingsOptions> settings) => _issuer = settings.Value.Authentication.JWT.Issuer;

        public Task<OpenIdConfigurationResponse> Handle(GetOpenIdConfigurationQuery request, CancellationToken cancellationToken)
            => Task.FromResult(new OpenIdConfigurationResponse(_issuer, request.JwksUri));
    }
}
