using AutoMapper;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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

        [Fact]
        public async Task GetAttendancesByDateRangeShouldFilterByUserName()
        {
            // Arrange
            var (repository, captured) = CreateRepositoryCapturingFind([]);

            // Act
            await repository.GetAttendancesByDateRange("jdoe", new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

            // Assert
            var filter = RenderFilter(captured.Filter);
            Assert.Equal("jdoe", filter["UserName"].AsString);
        }

        [Fact]
        public async Task GetAttendancesByDateRangeShouldFilterFromStartOfFromDayToStartOfDayAfterTo()
        {
            // Arrange
            var (repository, captured) = CreateRepositoryCapturingFind([]);

            // Act
            await repository.GetAttendancesByDateRange("jdoe", new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

            // Assert
            var date = RenderFilter(captured.Filter)["Date"].AsBsonDocument;
            Assert.Equal(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), date["$gte"].ToUniversalTime());
            Assert.Equal(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), date["$lt"].ToUniversalTime());
        }

        [Fact]
        public async Task GetAttendancesByDateRangeShouldCoverWholeDayWhenFromEqualsTo()
        {
            // Arrange
            var (repository, captured) = CreateRepositoryCapturingFind([]);

            // Act
            await repository.GetAttendancesByDateRange("jdoe", new DateOnly(2026, 3, 10), new DateOnly(2026, 3, 10));

            // Assert
            var date = RenderFilter(captured.Filter)["Date"].AsBsonDocument;
            Assert.Equal(new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc), date["$gte"].ToUniversalTime());
            Assert.Equal(new DateTime(2026, 3, 11, 0, 0, 0, DateTimeKind.Utc), date["$lt"].ToUniversalTime());
        }

        [Fact]
        public async Task GetAttendancesByDateRangeShouldSortByDateDescending()
        {
            // Arrange
            var (repository, captured) = CreateRepositoryCapturingFind([]);

            // Act
            await repository.GetAttendancesByDateRange("jdoe", new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

            // Assert
            Assert.NotNull(captured.Options?.Sort);
            var sort = captured.Options.Sort.Render(RenderArgs());
            Assert.Equal(new BsonDocument("Date", -1), sort);
        }

        [Fact]
        public async Task GetAttendancesByDateRangeShouldReturnMappedRecords()
        {
            // Arrange
            var attendanceMongoList = new List<AttendanceMongo>
            {
                new() { UserName = "jdoe", Date = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc), Attendance = true },
                new() { UserName = "jdoe", Date = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc), Attendance = false }
            };
            var attendanceRecordList = new List<AttendanceRecord>
            {
                new() { UserName = "jdoe", Date = new DateTime(2026, 3, 10, 8, 0, 0, DateTimeKind.Utc), Attendance = true },
                new() { UserName = "jdoe", Date = new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc), Attendance = false }
            };
            _mockMapper.Setup(m => m.Map<IEnumerable<AttendanceRecord>>(It.Is<IEnumerable<AttendanceMongo>>(l => l.SequenceEqual(attendanceMongoList))))
                       .Returns(attendanceRecordList);
            var (repository, _) = CreateRepositoryCapturingFind(attendanceMongoList);

            // Act
            var result = await repository.GetAttendancesByDateRange("jdoe", new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

            // Assert
            Assert.Equal(attendanceRecordList, result);
        }

        private sealed class CapturedFind
        {
            public FilterDefinition<AttendanceMongo> Filter { get; set; } = null!;
            public FindOptions<AttendanceMongo, AttendanceMongo> Options { get; set; } = null!;
        }

        private (AttendanceRepository Repository, CapturedFind Captured) CreateRepositoryCapturingFind(List<AttendanceMongo> documents)
        {
            var captured = new CapturedFind();
            var mockCursor = new Mock<IAsyncCursor<AttendanceMongo>>();
            mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                      .ReturnsAsync(true)
                      .ReturnsAsync(false);
            mockCursor.SetupGet(c => c.Current).Returns(documents);
            var mockCollection = new Mock<IMongoCollection<AttendanceMongo>>();
            mockCollection.Setup(c => c.FindAsync(It.IsAny<FilterDefinition<AttendanceMongo>>(), It.IsAny<FindOptions<AttendanceMongo, AttendanceMongo>>(), It.IsAny<CancellationToken>()))
                          .Callback<FilterDefinition<AttendanceMongo>, FindOptions<AttendanceMongo, AttendanceMongo>, CancellationToken>((filter, options, _) =>
                          {
                              captured.Filter = filter;
                              captured.Options = options;
                          })
                          .ReturnsAsync(mockCursor.Object);
            _mockContext.Setup(c => c.AttendanceCollection).Returns(mockCollection.Object);

            return (new AttendanceRepository(_mockContext.Object, _mockMapper.Object), captured);
        }

        private static RenderArgs<AttendanceMongo> RenderArgs()
            => new(BsonSerializer.SerializerRegistry.GetSerializer<AttendanceMongo>(), BsonSerializer.SerializerRegistry);

        private static BsonDocument RenderFilter(FilterDefinition<AttendanceMongo> filter)
            => filter.Render(RenderArgs());
    }
}
