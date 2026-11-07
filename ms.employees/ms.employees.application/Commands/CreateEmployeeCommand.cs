using MediatR;

namespace ms.employees.application.Commands
{
    /// <param name="Token">The caller's Authorization header, forwarded to ms.users to create the account.</param>
    public record CreateEmployeeCommand(string UserName, string FirstName, string LastName, string Password, string Role, string Token) : IRequest<string>;
}
