using MediatR;

namespace ms.users.application.Commands
{
    /// <summary>
    /// Replaces every readable stored password with its protected form. Returns how many were protected.
    /// </summary>
    public record ProtectStoredPasswordsCommand : IRequest<int>;
}
