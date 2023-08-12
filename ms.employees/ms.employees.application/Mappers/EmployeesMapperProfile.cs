using AutoMapper;
using ms.employees.application.Responses;
using ms.employees.domain.Entities;

namespace ms.employees.application.Mappers
{
    public class EmployeesMapperProfile : Profile
    {
        public EmployeesMapperProfile() =>
            CreateMap<Employee, EmployeeResponse>().ReverseMap();
    }
}
