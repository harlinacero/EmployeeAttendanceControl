using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using ms.attendances.application.Queries;
using ms.attendances.application.Queries.Handlers;
using ms.attendances.application.Responses;
using ms.attendances.domain.Entities;
using ms.attendances.domain.Repositories;

namespace ms.attendances.application.test.Queries
{
    public class GetAttendancesByDateRangeQueryHandlerTests
    {
        private readonly Mock<IAttendanceRepository> _attendanceRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<GetAttendancesByDateRangeQueryHandler>> _loggerMock;

        public GetAttendancesByDateRangeQueryHandlerTests()
        {
            _attendanceRepositoryMock = new Mock<IAttendanceRepository>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<GetAttendancesByDateRangeQueryHandler>>();
        }

        private GetAttendancesByDateRangeQueryHandler CreateHandler()
            => new(_attendanceRepositoryMock.Object, _mapperMock.Object, _loggerMock.Object);

        [Fact]
        public async Task HandleShouldReturnMappedAttendances()
        {
            // Arrange
            var from = new DateOnly(2026, 3, 1);
            var to = new DateOnly(2026, 3, 31);
            var query = new GetAttendancesByDateRangeQuery("jdoe", from, to);
            var attendanceRecords = new List<AttendanceRecord>
            {
                new() { UserName = "jdoe", Date = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc), Attendance = true },
                new() { UserName = "jdoe", Date = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc), Attendance = false }
            };
            var attendanceResponses = new List<AttendanceResponse>
            {
                new() { UserName = "jdoe", Date = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc), Attendance = true },
                new() { UserName = "jdoe", Date = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc), Attendance = false }
            };
            _attendanceRepositoryMock.Setup(r => r.GetAttendancesByDateRange("jdoe", from, to))
                                     .ReturnsAsync(attendanceRecords);
            _mapperMock.Setup(m => m.Map<IEnumerable<AttendanceResponse>>(attendanceRecords))
                       .Returns(attendanceResponses);

            // Act
            var result = await CreateHandler().Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(attendanceResponses, result);
            _attendanceRepositoryMock.Verify(r => r.GetAttendancesByDateRange("jdoe", from, to), Times.Once);
        }

        [Fact]
        public async Task HandleShouldReturnEmptyListWhenNoAttendancesInRange()
        {
            // Arrange
            var from = new DateOnly(2026, 9, 1);
            var to = new DateOnly(2026, 9, 30);
            var query = new GetAttendancesByDateRangeQuery("jdoe", from, to);
            var attendanceRecords = new List<AttendanceRecord>();
            _attendanceRepositoryMock.Setup(r => r.GetAttendancesByDateRange("jdoe", from, to))
                                     .ReturnsAsync(attendanceRecords);
            _mapperMock.Setup(m => m.Map<IEnumerable<AttendanceResponse>>(attendanceRecords))
                       .Returns(new List<AttendanceResponse>());

            // Act
            var result = await CreateHandler().Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task HandleShouldReturnEmptyListWhenEmployeeDoesNotExist()
        {
            // Arrange
            var from = new DateOnly(2026, 3, 1);
            var to = new DateOnly(2026, 3, 31);
            var query = new GetAttendancesByDateRangeQuery("ghost", from, to);
            var attendanceRecords = new List<AttendanceRecord>();
            _attendanceRepositoryMock.Setup(r => r.GetAttendancesByDateRange("ghost", from, to))
                                     .ReturnsAsync(attendanceRecords);
            _mapperMock.Setup(m => m.Map<IEnumerable<AttendanceResponse>>(attendanceRecords))
                       .Returns(new List<AttendanceResponse>());

            // Act
            var result = await CreateHandler().Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task HandleShouldLogErrorAndRethrowWhenRepositoryFails()
        {
            // Arrange
            var query = new GetAttendancesByDateRangeQuery("jdoe", new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));
            var exception = new TimeoutException("History unavailable");
            _attendanceRepositoryMock.Setup(r => r.GetAttendancesByDateRange(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
                                     .ThrowsAsync(exception);

            // Act
            var thrown = await Assert.ThrowsAsync<TimeoutException>(() => CreateHandler().Handle(query, CancellationToken.None));

            // Assert
            Assert.Same(exception, thrown);
            _loggerMock.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }
    }
}
