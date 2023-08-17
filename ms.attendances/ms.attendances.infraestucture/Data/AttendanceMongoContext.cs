using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using ms.attendances.infraestucture.MongoEntities;

namespace ms.attendances.infraestucture.Data
{
    public class AttendanceMongoContext : IAttendanceContext
    {
        private readonly IConfiguration _configuration;
        private IMongoDatabase _mongoDatabase;


        public AttendanceMongoContext(IConfiguration configuration)
        {
            _configuration = configuration;
            _mongoDatabase = new MongoClient(_configuration.GetConnectionString("MongoDB"))
                .GetDatabase(_configuration.GetConnectionString("AttendanceDB"));
        }

        public IMongoCollection<AttendanceMongo> AttendanceCollection =>
            _mongoDatabase.GetCollection<AttendanceMongo>(_configuration.GetConnectionString("AttendanceCollection"));
    }
}
