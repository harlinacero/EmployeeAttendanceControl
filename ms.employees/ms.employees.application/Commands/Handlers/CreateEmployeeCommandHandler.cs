using MediatR;
using Microsoft.Extensions.Logging;
using ms.employees.application.HttpComunications;
using ms.employees.application.HttpComunications.Requests;
using ms.employees.domain.Entities;
using ms.employees.domain.Exceptions;
using ms.employees.domain.Repositories;
using Refit;
using System.Net;

namespace ms.employees.application.Commands.Handlers
{
    /// <summary>
    /// Creates the account in ms.users first, so an employee never exists without a way to sign in.
    /// The password only passes through: it's neither stored, logged nor published.
    /// </summary>
    public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, string>
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IUsersApiCommunication _usersApiCommunication;
        private readonly ILogger<CreateEmployeeCommandHandler> _logger;

        public CreateEmployeeCommandHandler(IEmployeeRepository employeeRepository,
            IUsersApiCommunication usersApiCommunication, ILogger<CreateEmployeeCommandHandler> logger)
        {
            _employeeRepository = employeeRepository;
            _usersApiCommunication = usersApiCommunication;
            _logger = logger;
        }

        public async Task<string> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
        {
            await CreateAccount(request);

            var res = await _employeeRepository.CreateEmployee(new Employee
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                UserName = request.UserName
            });
            _logger.LogInformation("Employee {UserName} created", request.UserName);
            return res;
        }

        private async Task CreateAccount(CreateEmployeeCommand request)
        {
            try
            {
                await _usersApiCommunication.CreateAccount(new CreateAccountApiRequest(request.UserName, request.Password, request.Role), request.Token);
            }
            catch (ApiException exception) when (exception.StatusCode == HttpStatusCode.Conflict)
            {
                // Lets the administrator retry after the employee failed to save; the existing password stays
                _logger.LogWarning("Account {UserName} already exists: the employee is linked to it and its password is kept", request.UserName);
            }
            catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
            {
                throw new UsersServiceUnavailableException(exception);
            }
        }
    }
}
