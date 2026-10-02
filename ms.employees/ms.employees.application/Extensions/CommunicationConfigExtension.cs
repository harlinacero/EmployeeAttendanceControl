using Microsoft.Extensions.DependencyInjection;
using Refit;

using ms.employees.application.HttpComunications;
using ms.employees.application.Mappers;
using ms.employees.application.Queries.Handlers;
using ms.rabbitmq.Producers;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace ms.employees.application.Extensions
{
    public static class CommunicationConfigExtension
    {
        public static IServiceCollection AddComunicationsSettings(this IServiceCollection services)
        {
            var settings = services.BuildServiceProvider().GetRequiredService<IOptions<SettingsOptions>>();
            var communitationSettings = settings.Value.Communication;

            services.AddRefitClient<IAttendanceApiCommunication>().ConfigureHttpClient(c => 
                c.BaseAddress = new Uri(communitationSettings.External.AttendanceApiUrl)
            );

            services.AddAutoMapper(mapperConfig => mapperConfig.AddMaps(typeof(EmployeesMapperProfile).Assembly));

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(GetAllEmployeesQueryHandler).GetTypeInfo().Assembly);
            });
            services.AddSingleton(typeof(IProducer), typeof(EventProducer));

            return services;
        }
    }
}
