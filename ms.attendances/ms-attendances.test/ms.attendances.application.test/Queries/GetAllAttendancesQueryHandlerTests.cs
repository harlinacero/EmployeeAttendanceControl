using AutoMapper;
using Moq;
using ms.attendances.application.Queries;
using ms.attendances.application.Queries.Handlers;
using ms.attendances.application.Responses;
using ms.attendances.domain.Entities;
using ms.attendances.domain.Repositories;

namespace ms.attendances.application.test.Queries
{
    public class GetAllAttendancesQueryHandlerTests
    {
        private readonly Mock<IAttendanceRepository> _attendanceRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;

        public GetAllAttendancesQueryHandlerTests()
        {
            _attendanceRepositoryMock = new Mock<IAttendanceRepository>();
            _mapperMock = new Mock<IMapper>();
        }

        [Fact]
        public async Task HandleShouldReturnMappedAttendances()
        {
            // Arrange
            var userName = "testuser";
            var query = new GetAllAttendancesQuery(userName);
            var attendanceRecords = new List<AttendanceRecord>
            {
                new() { UserName = userName, FullName = "Test User 1", Date = DateTime.Now, Attendance = true, Notes = "Test notes 1" },
                new() { UserName = userName, FullName = "Test User 2", Date = DateTime.Now, Attendance = false, Notes = "Test notes 2" }
            };
            var attendanceResponses = new List<AttendanceResponse>
            {
                new() { UserName = userName, FullName = "Test User 1", Date = DateTime.Now, Attendance = true, Notes = "Test notes 1" },
                new() { UserName = userName, FullName = "Test User 2", Date = DateTime.Now, Attendance = false, Notes = "Test notes 2" }
            };
            _attendanceRepositoryMock.Setup(r => r.GetAllAttendances(userName))
                                     .ReturnsAsync(attendanceRecords);
            _mapperMock.Setup(m => m.Map<IEnumerable<AttendanceResponse>>(attendanceRecords))
                       .Returns(attendanceResponses);
            var handler = new GetAllAttendancesQueryHandler(_attendanceRepositoryMock.Object, _mapperMock.Object);
            
            // Act
            var result = await handler.Handle(query, CancellationToken.None);
            
            // Assert
            Assert.Equal(attendanceResponses.Count, result.Count());
        }
    }
}
