using AutoMapper;
using ms.employees.application.Commands;
using ms.employees.application.Events;
using ms.employees.application.Responses;
using ms.employees.domain.Entities;
using ms.rabbitmq.Events;

namespace ms.employees.application.Mappers
{
    public class EmployeesMapperProfile : Profile
    {
        public EmployeesMapperProfile()
        {
            CreateMap<Employee, EmployeeResponse>().ReverseMap();
            CreateMap<CreateEmployeeCommand, EmployeeCreateEvent>().ReverseMap();
            CreateMap<Employee, AttendanceStateChangedEvent>()
                .ForMember(dest => dest.Attendance, source => source.MapFrom(source => source.LastAttendanceState))
                .ForMember(dest => dest.Date, source => source.MapFrom(source => source.LastAttendanceDate))
                .ForMember(dest => dest.Notes, source => source.MapFrom(source => source.LastAttendanceNotes))
                .ForMember(dest => dest.FullName, source => source.MapFrom(source => string.Concat(source.FirstName, " ", source.LastName)))
                .ReverseMap();
        }
    }
}
