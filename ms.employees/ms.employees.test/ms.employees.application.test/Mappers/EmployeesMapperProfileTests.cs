using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using ms.employees.application.Commands;
using ms.employees.application.Events;
using ms.employees.application.Mappers;
using ms.employees.application.Responses;
using ms.employees.domain.Entities;

namespace ms.employees.application.test.Mappers
{
    public class EmployeesMapperProfileTests
    {
        private readonly IMapper _mapper;
        private readonly MapperConfiguration _configuration;
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;

        public EmployeesMapperProfileTests()
        {
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<EmployeesMapperProfile>();
            }, _loggerFactoryMock.Object);


            _mapper = _configuration.CreateMapper();

        }
        [Fact]
        public void EmployeesMapperProfileShouldHaveValidConfiguration()
        {
            // Act & Assert
            _configuration.AssertConfigurationIsValid();
        }

        [Fact]
        public void EmployeesMapperProfileShouldMapEmployeeToEmployeeResponse()
        {
            // Arrange
            var employee = new Employee
            {
                UserName = "testuser",
                FirstName = "Test",
                LastName = "Lastname",
                LastAttendanceDate = DateTime.Now,
                LastAttendanceState = true,
                LastAttendanceNotes = "Present"
            };
            // Act
            var employeeResponse = _mapper.Map<EmployeeResponse>(employee);
            // Assert
            Assert.Equal(employee.UserName, employeeResponse.UserName);
            Assert.Equal(employee.FirstName, employeeResponse.FirstName);
            Assert.Equal(employee.LastName, employeeResponse.LastName);
            Assert.Equal(employee.LastAttendanceDate, employeeResponse.LastAttendance);
            Assert.Equal(employee.LastAttendanceState, employeeResponse.LastAttendanceState);
            Assert.Equal(employee.LastAttendanceNotes, employeeResponse.LastAttendanceNotes);
        }

        [Fact]
        public void EmployeesMapperProfileShouldMapEmployeeResponseToEmployee()
        {
            // Arrange
            var employeeResponse = new EmployeeResponse
            {
                UserName = "testuser",
                FirstName = "Test",
                LastName = "Lastname",
                LastAttendance = DateTime.Now,
                LastAttendanceState = true,
                LastAttendanceNotes = "Present"
            };
            // Act
            var employee = _mapper.Map<Employee>(employeeResponse);
            // Assert
            Assert.Equal(employeeResponse.UserName, employee.UserName);
            Assert.Equal(employeeResponse.FirstName, employee.FirstName);
            Assert.Equal(employeeResponse.LastName, employee.LastName);
            Assert.Equal(employeeResponse.LastAttendance, employee.LastAttendanceDate);
            Assert.Equal(employeeResponse.LastAttendanceState, employee.LastAttendanceState);
            Assert.Equal(employeeResponse.LastAttendanceNotes, employee.LastAttendanceNotes);
        }

        [Fact]
        public void EmployeesMapperProfileShouldMapCreateEmployeeCommandToEmployeeCreateEvent()
        {
            // Arrange
            var createEmployeeCommand = new CreateEmployeeCommand("testuser", "Test", "Lastname", "password", "admin");
            // Act
            var employeeCreateEvent = _mapper.Map<EmployeeCreateEvent>(createEmployeeCommand);
            // Assert
            Assert.Equal(createEmployeeCommand.UserName, employeeCreateEvent.UserName);
            Assert.Equal(createEmployeeCommand.Password, employeeCreateEvent.Password);
            Assert.Equal(createEmployeeCommand.Role, employeeCreateEvent.Role);
        }

      

        [Fact]
        public void EmployeesMapperProfileShouldMapEmployeeToAttendanceStateChangedEvent()
        {
            // Arrange
            var employee = new Employee
            {
                UserName = "testuser",
                FirstName = "Test",
                LastName = "Lastname",
                LastAttendanceDate = DateTime.Now,
                LastAttendanceState = true,
                LastAttendanceNotes = "Present"
            };
            // Act
            var attendanceStateChangedEvent = _mapper.Map<AttendanceStateChangedEvent>(employee);
            // Assert
            Assert.Equal(employee.LastAttendanceDate, attendanceStateChangedEvent.Date);
            Assert.Equal(employee.LastAttendanceState, attendanceStateChangedEvent.Attendance);
            Assert.Equal(employee.LastAttendanceNotes, attendanceStateChangedEvent.Notes);
            Assert.Equal(string.Concat(employee.FirstName, " ", employee.LastName), attendanceStateChangedEvent.FullName);
        }

        [Fact]
        public void EmployeesMapperProfileShouldMapAttendanceStateChangedEventToEmployee()
        {
            // Arrange
            var attendanceStateChangedEvent = new AttendanceStateChangedEvent
            {
                Date = DateTime.Now,
                Attendance = true,
                Notes = "Present",
                FullName = "Test Lastname"
            };
            // Act
            var employee = _mapper.Map<Employee>(attendanceStateChangedEvent);
            // Assert
            Assert.Equal(attendanceStateChangedEvent.Date, employee.LastAttendanceDate);
            Assert.Equal(attendanceStateChangedEvent.Attendance, employee.LastAttendanceState);
            Assert.Equal(attendanceStateChangedEvent.Notes, employee.LastAttendanceNotes);
        }
    }
}
