using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using ms.attendances.api.Events;
using ms.attendances.api.Mappers;
using ms.attendances.application.Request;

namespace ms_attendances.api.test.Mappers
{
    public class EventMapperProfileTests
    {
        private readonly IMapper _mapper;
        private readonly MapperConfiguration _configuration;
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;

        public EventMapperProfileTests()
        {
            MapperConfigurationExpression expression = new();
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<EventMapperProfile>();
            }, _loggerFactoryMock.Object);

            _mapper = _configuration.CreateMapper();
        }

        [Fact]
        public void ShouldMapCreateAttendanceRequestToAttendanceStateChangedEvent()
        {
            // Arrange
            CreateAttendanceRequest request = new()
            {
                FullName = "John Doe",
                Date = DateTime.Now,
                Attendance = true,
                Notes = "Present"
            };

            // Act
            var result = _mapper.Map<AttendanceStateChangedEvent>(request);

            // Assert
            Assert.Equal(request.FullName, result.FullName);
            Assert.Equal(request.Date, result.Date);
            Assert.Equal(request.Attendance, result.Attendance);
            Assert.Equal(request.Notes, result.Notes);
        }

        [Fact]
        public void ShouldMapAttendanceStateChangedEventToCreateAttendanceRequest()
        {
            // Arrange
            AttendanceStateChangedEvent evento = new()
            {
                FullName = "John Doe",
                Date = DateTime.Now,
                Attendance = true,
                Notes = "Present"
            };
            // Act
            var request = _mapper.Map<CreateAttendanceRequest>(evento);
            // Assert
            Assert.Equal(request.FullName, evento.FullName);
            Assert.Equal(request.Date, evento.Date);
            Assert.Equal(request.Attendance, evento.Attendance);
            Assert.Equal(request.Notes, evento.Notes);
        }
    }
}
