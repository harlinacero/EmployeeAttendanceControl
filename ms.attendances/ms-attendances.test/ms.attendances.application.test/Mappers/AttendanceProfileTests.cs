using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using ms.attendances.application.Mappers;
using ms.attendances.application.Request;
using ms.attendances.application.Responses;
using ms.attendances.domain.Entities;

namespace ms.attendances.application.test.Mappers
{
    public class AttendanceProfileTests
    {

        private readonly IMapper _mapper;
        private readonly MapperConfiguration _configuration;
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;
        public AttendanceProfileTests()
        {
            _loggerFactoryMock = new Mock<ILoggerFactory>();

            _configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AttendanceProfile>();
            }, _loggerFactoryMock.Object);

            _mapper = _configuration.CreateMapper();
        }

        [Fact]
        public void AttendanceProfile_ShouldHaveValidConfiguration()
        {
            // Act & Assert
            _configuration.AssertConfigurationIsValid();
        }

        [Fact]
        public void ShouldMapAttendanceRecordToAttendanceResponse()
        {
            // Arrange
            var attendanceRecord = new AttendanceRecord
            {
                UserName = "testuser",
                FullName = "Test User",
                Date = DateTime.Now,
                Attendance = true,
                Notes = "Test notes"
            };
            // Act
            var result = _mapper.Map<AttendanceResponse>(attendanceRecord);
            // Assert
            Assert.Equal(attendanceRecord.UserName, result.UserName);
            Assert.Equal(attendanceRecord.FullName, result.FullName);
            Assert.Equal(attendanceRecord.Date, result.Date);
            Assert.Equal(attendanceRecord.Attendance, result.Attendance);
            Assert.Equal(attendanceRecord.Notes, result.Notes);
        }

        [Fact]
        public void ShouldMapAttendanceResponseToAttendanceRecord()
        {
            // Arrange
            var attendanceResponse = new AttendanceResponse
            {
                UserName = "testuser",
                FullName = "Test User",
                Date = DateTime.Now,
                Attendance = true,
                Notes = "Test notes"
            };
            // Act
            var result = _mapper.Map<AttendanceRecord>(attendanceResponse);
            // Assert
            Assert.Equal(attendanceResponse.UserName, result.UserName);
            Assert.Equal(attendanceResponse.FullName, result.FullName);
            Assert.Equal(attendanceResponse.Date, result.Date);
            Assert.Equal(attendanceResponse.Attendance, result.Attendance);
            Assert.Equal(attendanceResponse.Notes, result.Notes);
        }

        [Fact]
        public void ShouldMapAttendanceRecordToAttendanceStateChangedEvent()
        {
            // Arrange
            var attendanceRecord = new AttendanceRecord
            {
                UserName = "testuser",
                FullName = "Test User",
                Date = DateTime.Now,
                Attendance = true,
                Notes = "Test notes"
            };
            // Act
            var result = _mapper.Map<CreateAttendanceRequest>(attendanceRecord);
            
            // Assert
            Assert.Equal(attendanceRecord.FullName, result.FullName);
            Assert.Equal(attendanceRecord.Date, result.Date);
            Assert.Equal(attendanceRecord.Attendance, result.Attendance);
            Assert.Equal(attendanceRecord.Notes, result.Notes);
        }

        [Fact]
        public void ShouldMapAttendanceStateChangedEventToAttendanceRecord()
        {
            // Arrange
            var attendanceRequest = new CreateAttendanceRequest
            {
                FullName = "Test User",
                Date = DateTime.Now,
                Attendance = true,
                Notes = "Test notes"
            };
            // Act
            var result = _mapper.Map<AttendanceRecord>(attendanceRequest);

            // Assert
            Assert.Equal(attendanceRequest.FullName, result.FullName);
            Assert.Equal(attendanceRequest.Date, result.Date);
            Assert.Equal(attendanceRequest.Attendance, result.Attendance);
            Assert.Equal(attendanceRequest.Notes, result.Notes);
        }
    }
}
