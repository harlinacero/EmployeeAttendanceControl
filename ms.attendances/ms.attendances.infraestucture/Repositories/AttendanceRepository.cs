using AutoMapper;
using MongoDB.Driver;
using ms.attendances.domain.Entities;
using ms.attendances.domain.Repositories;
using ms.attendances.infraestucture.Data;
using ms.attendances.infraestucture.MongoEntities;

namespace ms.attendances.infraestucture.Repositories
{
    public class AttendanceRepository : IAttendanceRepository
    {
        private readonly IAttendanceContext _context;
        private readonly IMapper _mapper;

        public AttendanceRepository(IAttendanceContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<bool> CreateAttendance(AttendanceRecord record)
        {
            await _context.AttendanceCollection.InsertOneAsync(_mapper.Map<AttendanceMongo>(record));
            return true;
        }

        public async Task<IEnumerable<AttendanceRecord>> GetAllAttendances(string userName)
        {
            var filter = Builders<AttendanceMongo>.Filter.Eq(a => a.UserName, userName);
            var queryResult = await ((!string.IsNullOrEmpty(userName)) ? 
                _context.AttendanceCollection.FindAsync(filter)
                : _context.AttendanceCollection.FindAsync(Builders<AttendanceMongo>.Filter.Empty));
            
            var res = queryResult.ToListAsync().Result;
            return _mapper.Map<IEnumerable<AttendanceRecord>>(res);
        }
    }
}
