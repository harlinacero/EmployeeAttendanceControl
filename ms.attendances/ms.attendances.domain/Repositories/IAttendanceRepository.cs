using ms.attendances.domain.Entities;

namespace ms.attendances.domain.Repositories
{
    public interface IAttendanceRepository
    {
        Task<IEnumerable<AttendanceRecord>> GetAllAttendances(string userName);
        Task<IEnumerable<AttendanceRecord>> GetAttendancesByDateRange(string userName, DateOnly from, DateOnly to);
        Task<bool> CreateAttendance(AttendanceRecord record);
    }
}
