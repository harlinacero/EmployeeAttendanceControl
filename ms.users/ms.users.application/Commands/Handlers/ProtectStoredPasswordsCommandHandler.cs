using MediatR;
using Microsoft.Extensions.Logging;
using ms.users.application.Security;
using ms.users.domain.Interfaces;

namespace ms.users.application.Commands.Handlers
{
    /// <summary>
    /// Runs on startup. Each account is updated only if its value didn't change meanwhile, so running it again is harmless.
    /// Logs counts only: never a password or a hash.
    /// </summary>
    public class ProtectStoredPasswordsCommandHandler : IRequestHandler<ProtectStoredPasswordsCommand, int>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<ProtectStoredPasswordsCommandHandler> _logger;

        public ProtectStoredPasswordsCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, ILogger<ProtectStoredPasswordsCommandHandler> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<int> Handle(ProtectStoredPasswordsCommand request, CancellationToken cancellationToken)
        {
            var users = await _userRepository.GetAllUsers();
            var readable = users.Where(user => !string.IsNullOrEmpty(user.PasswordHash) && !_passwordHasher.IsProtected(user.PasswordHash)).ToList();

            var protectedCount = 0;
            foreach (var user in readable)
            {
                if (await _userRepository.ProtectPassword(user.UserName, user.PasswordHash, _passwordHasher.Hash(user.PasswordHash)))
                {
                    protectedCount++;
                }
            }

            _logger.LogInformation("Protected {ProtectedCount} stored passwords out of {UserCount} accounts", protectedCount, users.Count);
            return protectedCount;
        }
    }
}
