using MediatR;
using ms.employees.domain.Entities;
using ms.employees.domain.Repositories;

namespace ms.employees.application.Commands.Handlers
{
    public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, string>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public CreateEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<string> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            return await _employeeRepository.CreateEmployee(new Employee
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserName = request.UserName
            });
        }
    }
}
