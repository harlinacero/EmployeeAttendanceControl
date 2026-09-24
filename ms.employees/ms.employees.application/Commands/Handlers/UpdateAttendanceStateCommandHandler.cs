using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ms.employees.application.Events;
using ms.employees.application.HttpComunications;
using ms.employees.domain.Repositories;
using ms.rabbitmq.Events;
using ms.rabbitmq.Producers;

namespace ms.employees.application.Commands.Handlers
{
    public class UpdateAttendanceStateCommandHandler : IRequestHandler<UpdateAttendanceStateCommand, string>
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IProducer _producer;
        private readonly IMapper _mapper;
        private readonly IAttendanceApiCommunication _attendanceApiCommunication;
        private readonly ILogger<UpdateAttendanceStateCommandHandler> _logger;

        public UpdateAttendanceStateCommandHandler(IEmployeeRepository employeeRepository,
            IProducer producer,
            IMapper mapper,
            IAttendanceApiCommunication attendanceApiCommunication, 
            ILogger<UpdateAttendanceStateCommandHandler> logger)
        {
            _employeeRepository = employeeRepository;
            _producer = producer;
            _mapper = mapper;
            _attendanceApiCommunication = attendanceApiCommunication;
            _logger = logger;
        }

        public async Task<string> Handle(UpdateAttendanceStateCommand request, CancellationToken cancellationToken)
        {
            var userAttendances = await _attendanceApiCommunication.GetAllAttendances(request.UserName, request.Token);
            var numberOfAttendances = userAttendances.Count();

            string notes = request.Notes == null ? $"[{numberOfAttendances} Asistencias]" : string.Concat(request.Notes, $" [{numberOfAttendances}] Asistencias");

            var res = await _employeeRepository.UpdateAttendanceStateEmployee(request.UserName, request.Attendance, notes);
            _logger.LogInformation($"Attendance state for employee '{request.UserName}' updated to '{request.Attendance}' with notes: '{notes}'. Response: {res}");
            var employee = await _employeeRepository.GetEmployee(request.UserName) ?? throw new KeyNotFoundException($"Employee '{request.UserName}' was not found after updating attendance.");
            await _producer.Produce(_mapper.Map<AttendanceStateChangedEvent>(employee));
            _logger.LogInformation($"AttendanceStateChangedEvent produced for employee '{request.UserName}'.");
            return res;
        }
    }
}
