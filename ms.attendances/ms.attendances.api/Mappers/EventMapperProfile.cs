using AutoMapper;
using ms.attendances.api.Events;
using ms.attendances.application.Request;

namespace ms.attendances.api.Mappers
{
    public class EventMapperProfile : Profile
    {
        public EventMapperProfile()
        {
            CreateMap<CreateAttendanceRequest, AttendanceStateChangedEvent>().ReverseMap();
        }
    }
}
