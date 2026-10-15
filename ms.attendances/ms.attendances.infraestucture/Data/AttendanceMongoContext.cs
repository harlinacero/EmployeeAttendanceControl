using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ms.attendances.infraestucture.Extensions;
using ms.attendances.infraestucture.MongoEntities;

namespace ms.attendances.infraestucture.Data
{
    public class AttendanceMongoContext : IAttendanceContext
    {
        private readonly ConnectionStrings _connectionStrings;
        private IMongoDatabase _mongoDatabase;


        public AttendanceMongoContext(IOptions<ConnectionStrings> configuration)
        {
            _connectionStrings = configuration.Value;
            _mongoDatabase = new MongoClient(_connectionStrings.MongoDB)
                .GetDatabase(_connectionStrings.AttendanceDB);
        }

        public IMongoCollection<AttendanceMongo> AttendanceCollection =>
            _mongoDatabase.GetCollection<AttendanceMongo>(_connectionStrings.AttendanceCollection);
    }
}
