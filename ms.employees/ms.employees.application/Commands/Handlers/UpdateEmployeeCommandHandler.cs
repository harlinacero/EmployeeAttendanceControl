using MediatR;
using ms.employees.domain.Repositories;

namespace ms.employees.application.Commands.Handlers
{
    public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, string>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public UpdateEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<string> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
        {
            return await _employeeRepository.UpdateAttendanceStateEmp(request.UserName, request.Attendance, request.Notes);
        }
    }
}
