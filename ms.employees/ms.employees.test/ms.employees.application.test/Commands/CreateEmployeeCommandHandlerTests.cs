using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using ms.employees.application.Commands;
using ms.employees.application.Commands.Handlers;
using ms.employees.application.Events;
using ms.employees.domain.Entities;
using ms.employees.domain.Repositories;
using ms.rabbitmq.Producers;


namespace ms.employees.application.test.Commands
{
    public class CreateEmployeeCommandHandlerTests
    {
        private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
        private readonly Mock<IProducer> _producerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<CreateEmployeeCommandHandler>> _loggerMock;

        public CreateEmployeeCommandHandlerTests()
        {
            _employeeRepositoryMock = new Mock<IEmployeeRepository>();
            _producerMock = new Mock<IProducer>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<CreateEmployeeCommandHandler>>();
        }

        [Fact]
        public async Task HandleShouldCreateEmployeeAndSendEvent()
        {
            // Arrange
            var command = new CreateEmployeeCommand("testuser", "Test", "Lastname", "password", "admin");

            _employeeRepositoryMock.Setup(repo => repo.CreateEmployee(It.IsAny<Employee>()))
                .ReturnsAsync(command.UserName);
            var handler = new CreateEmployeeCommandHandler(
                _employeeRepositoryMock.Object,
                _producerMock.Object,
                _mapperMock.Object,
                _loggerMock.Object);
            // Act
            var result = await handler.Handle(command, CancellationToken.None);
            // Assert
            Assert.Equal(command.UserName, result);
            _employeeRepositoryMock.Verify(repo => repo.CreateEmployee(It.IsAny<Employee>()), Times.Once);
            _producerMock.Verify(producer => producer.Produce(It.IsAny<EmployeeCreateEvent>()), Times.Once);
        }


    }
}
