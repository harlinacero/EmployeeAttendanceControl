using AutoMapper;
using MongoDB.Driver;
using Moq;
using ms.attendances.domain.Entities;
using ms.attendances.infraestucture.Data;
using ms.attendances.infraestucture.MongoEntities;
using ms.attendances.infraestucture.Repositories;

namespace ms.attendances.infraestucture.test.Repositories
{
    public class AttendanceRepositoryTests
    {
        private readonly Mock<IAttendanceContext> _mockContext;
        private readonly Mock<IMapper> _mockMapper;

        public AttendanceRepositoryTests()
        {
            _mockContext = new Mock<IAttendanceContext>();
            _mockMapper = new Mock<IMapper>();
        }

        [Fact]
        public async Task CreateAttendance_ShouldReturnTrue_WhenAttendanceIsCreated()
        {
            // Arrange
            var attendanceRecord = new AttendanceRecord
            {
                FullName = "John Doe",
                Date = DateTime.Now,
                Attendance = true,
                Notes = "Present"
            };
            var attendanceMongo = new AttendanceMongo
            {
                FullName = "John Doe",
                Date = DateTime.Now,
                Attendance = true,
                Notes = "Present"
            };
            _mockMapper.Setup(m => m.Map<AttendanceMongo>(attendanceRecord))
                       .Returns(attendanceMongo);
            var mockCollection = new Mock<IMongoCollection<AttendanceMongo>>();
            _mockContext.Setup(c => c.AttendanceCollection).Returns(mockCollection.Object);
            
            var repository = new AttendanceRepository(_mockContext.Object, _mockMapper.Object);
            
            // Act
            var result = await repository.CreateAttendance(attendanceRecord);
            
            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetAllAttendances_ShouldReturnMappedRecords_WhenUserNameIsProvided()
        {
            // Arrange
            var userName = "John Doe";
            var attendanceMongoList = new List<AttendanceMongo>
            {
                new() { FullName = "John Doe", Date = DateTime.Now, Attendance = true, Notes = "Present" }
            };
            var attendanceRecordList = new List<AttendanceRecord>
            {
                new() { FullName = "John Doe", Date = DateTime.Now, Attendance = true, Notes = "Present" }
            };

            _mockMapper.Setup(m => m.Map<IEnumerable<AttendanceRecord>>(It.IsAny<IEnumerable<AttendanceMongo>>()))
                       .Returns(attendanceRecordList);

            var mockCollection = new Mock<IMongoCollection<AttendanceMongo>>();
            var mockCursor = new Mock<IAsyncCursor<AttendanceMongo>>();
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                      .Returns(true)
                      .Returns(false);
            mockCursor.SetupGet(c => c.Current).Returns(attendanceMongoList);
            mockCollection.Setup(c => c.FindAsync(It.IsAny<FilterDefinition<AttendanceMongo>>(), It.IsAny<FindOptions<AttendanceMongo, AttendanceMongo>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(mockCursor.Object);

            _mockContext.Setup(c => c.AttendanceCollection).Returns(mockCollection.Object);

            var repository = new AttendanceRepository(_mockContext.Object, _mockMapper.Object);

            // Act
            var result = await repository.GetAllAttendances(userName);

            // Assert
            Assert.Equal(attendanceRecordList, result);
        }

        [Fact]
        public async Task GetAllAttendances_ShouldReturnMappedRecords_WhenUserNameIsEmpty()
        {
            // Arrange
            var attendanceMongoList = new List<AttendanceMongo>
            {
                new() { FullName = "John Doe", Date = DateTime.Now, Attendance = true, Notes = "Present" }
            };
            var attendanceRecordList = new List<AttendanceRecord>
            {
                new() { FullName = "John Doe", Date = DateTime.Now, Attendance = true, Notes = "Present" }
            };
            _mockMapper.Setup(m => m.Map<IEnumerable<AttendanceRecord>>(It.IsAny<IEnumerable<AttendanceMongo>>()))
                       .Returns(attendanceRecordList);
            var mockCollection = new Mock<IMongoCollection<AttendanceMongo>>();
            var mockCursor = new Mock<IAsyncCursor<AttendanceMongo>>();
            mockCursor.SetupSequence(c => c.MoveNext(It.IsAny<CancellationToken>()))
                      .Returns(true)
                      .Returns(false);
            mockCursor.SetupGet(c => c.Current).Returns(attendanceMongoList);
            mockCollection.Setup(c => c.FindAsync(It.IsAny<FilterDefinition<AttendanceMongo>>(), It.IsAny<FindOptions<AttendanceMongo, AttendanceMongo>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(mockCursor.Object);
            _mockContext.Setup(c => c.AttendanceCollection).Returns(mockCollection.Object);
            var repository = new AttendanceRepository(_mockContext.Object, _mockMapper.Object);
            // Act
            var result = await repository.GetAllAttendances(string.Empty);
            // Assert
            Assert.Equal(attendanceRecordList, result);
        }

    }
}
