using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ms.employees.application.Events;
using ms.employees.domain.Entities;
using ms.employees.domain.Repositories;
using ms.rabbitmq.Producers;

namespace ms.employees.application.Commands.Handlers
{
    public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, string>
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IProducer _producer;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateEmployeeCommandHandler> _logger;
        public CreateEmployeeCommandHandler(IEmployeeRepository employeeRepository,
            IProducer producer, IMapper mapper, ILogger<CreateEmployeeCommandHandler> logger)
        {
            _employeeRepository = employeeRepository;
            _producer = producer;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<string> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            var res = await _employeeRepository.CreateEmployee(new Employee
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserName = request.UserName
            });
            _logger.LogInformation($"Employee {request.UserName} created");
            _producer.Produce(_mapper.Map<EmployeeCreateEvent>(request));
            _logger.LogInformation($"Send event {nameof(EmployeeCreateEvent)} user {request.UserName}");
            return res;
        }
    }
}
