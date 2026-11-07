using MediatR;

namespace ms.users.application.Commands
{
    public record LogoutCommand(string RefreshToken) : IRequest;
}
