using MediatR;
using Microsoft.Extensions.Logging;
using ms.users.domain.Entities;
using ms.users.domain.Interfaces;

namespace ms.users.application.Commands.Handlers
{
    public class CreateUserAccountCommandHandler : IRequestHandler<CreateUserAccountCommand, string>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<CreateUserAccountCommandHandler> _logger;

        public CreateUserAccountCommandHandler(IUserRepository userRepository, ILogger<CreateUserAccountCommandHandler> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<string> Handle(CreateUserAccountCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.CreateUser(new User
            {
                UserName = request.UserName,
                Password = request.Password,
                Role = request.Role
            });
            _logger.LogInformation($"User {user.UserName} created");
            return user.UserName;
        }
    }
}
