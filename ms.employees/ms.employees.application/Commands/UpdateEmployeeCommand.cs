using MediatR;

namespace ms.employees.application.Commands
{
    public record UpdateEmployeeCommand(string UserName, bool Attendance, string Notes, string Token) : IRequest<string>;
}
