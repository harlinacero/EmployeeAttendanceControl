using Microsoft.Extensions.DependencyInjection;
using ms.attendances.domain.Repositories;
using ms.attendances.infraestucture.Data;
using ms.attendances.infraestucture.Repositories;

namespace ms.attendances.infraestucture.Extensions
{
    public static class Extensions
    {
        public static IServiceCollection AddAttendanceDbContext(this IServiceCollection services)
        {
            services.AddTransient<IAttendanceContext, AttendanceMongoContext>();
            services.AddTransient<IAttendanceRepository, AttendanceRepository>();

            return services;
        }
    }
}
