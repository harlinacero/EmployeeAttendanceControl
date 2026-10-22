using MediatR;
using ms.attendances.application.Responses;

namespace ms.attendances.application.Queries
{
    public record GetAttendancesByDateRangeQuery(string UserName, DateOnly From, DateOnly To) : IRequest<IEnumerable<AttendanceResponse>>;
}
