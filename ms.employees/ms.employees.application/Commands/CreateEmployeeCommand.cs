using MediatR;

namespace ms.employees.application.Commands
{
    public record CreateEmployeeCommand(string UserName, string FirstName, string LastName) : IRequest<string>;
}
