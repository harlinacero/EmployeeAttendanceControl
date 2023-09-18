using AutoMapper;
using ms.users.api.Events;
using ms.users.application.Commands;

namespace ms.users.api.Mappers
{
    public class EventMapperProfile : Profile
    {
        public EventMapperProfile()
        {
            CreateMap<CreateUserAccountCommand, EmployeeCreateEvent>().ReverseMap();
        }
    }
}
