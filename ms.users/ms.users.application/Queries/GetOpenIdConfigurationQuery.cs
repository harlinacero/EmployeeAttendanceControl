using MediatR;
using ms.users.application.Responses;

namespace ms.users.application.Queries
{
    public record GetOpenIdConfigurationQuery(string JwksUri) : IRequest<OpenIdConfigurationResponse>;
}
