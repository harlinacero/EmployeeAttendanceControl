using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using ms.attendances.application.Responses;
using ms.attendances.domain.Repositories;

namespace ms.attendances.application.Queries.Handlers
{
    public class GetAttendancesByDateRangeQueryHandler : IRequestHandler<GetAttendancesByDateRangeQuery, IEnumerable<AttendanceResponse>>
    {
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAttendancesByDateRangeQueryHandler> _logger;

        public GetAttendancesByDateRangeQueryHandler(IAttendanceRepository attendanceRepository, IMapper mapper, ILogger<GetAttendancesByDateRangeQueryHandler> logger)
        {
            _attendanceRepository = attendanceRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<AttendanceResponse>> Handle(GetAttendancesByDateRangeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var attendances = await _attendanceRepository.GetAttendancesByDateRange(request.UserName, request.From, request.To);
                return _mapper.Map<IEnumerable<AttendanceResponse>>(attendances);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting attendances for {UserName} between {From} and {To}", request.UserName, request.From, request.To);
                throw;
            }
        }
    }
}
