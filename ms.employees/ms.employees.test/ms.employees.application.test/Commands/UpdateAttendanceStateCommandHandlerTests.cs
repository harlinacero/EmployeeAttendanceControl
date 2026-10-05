using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using ms.employees.application.Commands;
using ms.employees.application.Commands.Handlers;
using ms.employees.application.Events;
using ms.employees.application.HttpComunications;
using ms.employees.domain.Entities;
using ms.employees.domain.Repositories;
using ms.rabbitmq.Producers;


namespace ms.employees.application.test.Commands
{
    public class UpdateAttendanceStateCommandHandlerTests
    {
        private readonly Mock<IEmployeeRepository> _employeeRepositoryMock;
        private readonly Mock<IProducer> _producerMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<UpdateAttendanceStateCommandHandler>> _loggerMock;
        private readonly Mock<IAttendanceApiCommunication> _attendanceApiCommunicationMock;
        public UpdateAttendanceStateCommandHandlerTests()
        {
            _employeeRepositoryMock = new Mock<IEmployeeRepository>();
            _producerMock = new Mock<IProducer>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<UpdateAttendanceStateCommandHandler>>();
            _attendanceApiCommunicationMock = new Mock<IAttendanceApiCommunication>();
        }
        [Fact]
        public async Task HandleShouldUpdateAttendanceStateAndSendEvent()
        {
            // Arrange
            var command = new UpdateAttendanceStateCommand("testuser", true, "Some notes", "token");
            var userAttendances = new List<object> { new { } }; // Simulate one attendance record

            _employeeRepositoryMock.Setup(repo => repo.UpdateAttendanceStateEmployee(command.UserName, command.Attendance, It.IsAny<string>()))
                .ReturnsAsync(command.UserName);
            _employeeRepositoryMock.Setup(repo => repo.GetEmployee(command.UserName))
                .ReturnsAsync(new Employee { UserName = command.UserName });

            var handler = new UpdateAttendanceStateCommandHandler(
                _employeeRepositoryMock.Object,
                _producerMock.Object,
                _mapperMock.Object,
                _attendanceApiCommunicationMock.Object,
                _loggerMock.Object);
            // Act
            var result = await handler.Handle(command, CancellationToken.None);
            // Assert
            Assert.Equal(command.UserName, result);
            _employeeRepositoryMock.Verify(repo => repo.UpdateAttendanceStateEmployee(command.UserName, command.Attendance, It.IsAny<string>()), Times.Once);
            _producerMock.Verify(producer => producer.Produce(It.IsAny<AttendanceStateChangedEvent>()), Times.Once);
        }
    }
}
