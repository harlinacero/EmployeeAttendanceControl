using AutoMapper;
using MediatR;
using ms.employees.application.Events;
using ms.employees.application.HttpComunications;
using ms.employees.domain.Repositories;
using ms.rabbitmq.Events;
using ms.rabbitmq.Producers;

namespace ms.employees.application.Commands.Handlers
{
    public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, string>
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IProducer _producer;
        private readonly IMapper _mapper;
        private readonly IAttendanceApiCommunication _attendanceApiCommunication;

        public UpdateEmployeeCommandHandler(IEmployeeRepository employeeRepository, 
            IProducer producer, 
            IMapper mapper, 
            IAttendanceApiCommunication attendanceApiCommunication = null)
        {
            _employeeRepository = employeeRepository;
            _producer = producer;
            _mapper = mapper;
            _attendanceApiCommunication = attendanceApiCommunication;
        }

        public async Task<string> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var userAttendances = await _attendanceApiCommunication.GetAllAttendances(request.UserName, request.Token);
            var numberOfAttendances = userAttendances.Count(); 

            string notes = request.Notes == null ? $"[{numberOfAttendances} Asistencias]" : string.Concat(request.Notes, $" [{numberOfAttendances}] Asistencias");
            
            var res = await _employeeRepository.UpdateAttendanceStateEmployee(request.UserName, request.Attendance, notes);
            var employee = await _employeeRepository.GetEmployee(request.UserName);

            _producer.Produce(_mapper.Map<AttendanceStateChangedEvent>(employee));
            return res;
        }
    }
}
