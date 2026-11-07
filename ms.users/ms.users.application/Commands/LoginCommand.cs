using MediatR;
using ms.users.application.Responses;

namespace ms.users.application.Commands
{
    public record LoginCommand(string UserName, string Password) : IRequest<TokenResponse>;
}
