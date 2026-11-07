using MediatR;
using ms.users.application.Responses;

namespace ms.users.application.Commands
{
    public record RefreshSessionCommand(string RefreshToken) : IRequest<TokenResponse>;
}
