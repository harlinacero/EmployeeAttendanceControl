using MongoDB.Driver;
using ms.attendances.infraestucture.MongoEntities;

namespace ms.attendances.infraestucture.Data
{
    public interface IAttendanceContext
    {
        IMongoCollection<AttendanceMongo> AttendanceCollection { get; }
    }
}
