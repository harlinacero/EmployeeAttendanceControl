using ms.attendances.domain.Entities;

namespace ms.attendances.domain.Repositories
{
    public interface IAttendanceRepository
    {
        Task<IEnumerable<AttendanceRecord>> GetAllAttendances(string userName);
        Task<bool> CreateAttendance(AttendanceRecord record);
    }
}
