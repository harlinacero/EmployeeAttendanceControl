using AutoMapper;
using Moq;
using ms.attendances.application.Commands;
using ms.attendances.application.Commands.Handlers;
using ms.attendances.application.Request;
using ms.attendances.domain.Entities;
using ms.attendances.domain.Repositories;

namespace ms.attendances.application.test.Commands
{
    public class CreateAttendanceCommandHandlerTests
    {
        private readonly Mock<IAttendanceRepository> _attendanceRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;

        public CreateAttendanceCommandHandlerTests()
        {
            _attendanceRepositoryMock = new Mock<IAttendanceRepository>();
            _mapperMock = new Mock<IMapper>();
        }

        [Fact]
        public async Task HandleShouldReturnTrueWhenAttendanceIsCreatedSuccessfully()
        {
            // Arrange
            var attendanceRequest = new CreateAttendanceRequest
            {
                FullName = "Test User",
                Date = DateTime.Now,
                Attendance = true,
                Notes = "Test notes"
            };
            var command = new CreateAttendanceCommand("testuser", attendanceRequest);
            var attendanceRecord = new AttendanceRecord
            {
                UserName = command.UserName,
                FullName = command.AttendanceRequest.FullName,
                Date = command.AttendanceRequest.Date,
                Attendance = command.AttendanceRequest.Attendance,
                Notes = command.AttendanceRequest.Notes
            };
            _mapperMock.Setup(m => m.Map<AttendanceRecord>(command.AttendanceRequest))
                       .Returns(attendanceRecord);
            _attendanceRepositoryMock.Setup(r => r.CreateAttendance(attendanceRecord))
                                     .ReturnsAsync(true);
            var handler = new CreateAttendanceCommandHandler(_attendanceRepositoryMock.Object, _mapperMock.Object);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);
            
            // Assert
            Assert.True(result);
        }
    }
}
