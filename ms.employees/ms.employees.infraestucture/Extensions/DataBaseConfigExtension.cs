using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ms.employees.domain.Repositories;
using ms.employees.infraestucture.Data;
using ms.employees.infraestucture.Repositories;

namespace ms.employees.infraestucture.Extensions
{
    public static class DataBaseConfigExtension
    {
        public static IServiceCollection AddDataBaseExtension(this IServiceCollection services)
        {

            services.AddScoped<IDapperContext, EmployeesDapperContext>();
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();

            return services;
        }

    }
}
