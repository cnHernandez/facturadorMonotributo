using System.Reflection;
using Back.Repositories;

namespace Back.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServicesFromAssembly(this IServiceCollection services, Assembly assembly)
        {
            var types = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && (t.Name.EndsWith("Service") || t.Name.EndsWith("Services")))
                .ToList();

            foreach (var type in types)
            {
                var interfaces = type.GetInterfaces();
                if (interfaces.Any())
                {
                    foreach (var @interface in interfaces)
                    {
                        services.AddScoped(@interface, type);
                    }
                }
            }

            return services;
        }

        public static IServiceCollection AddApplicationRepository(this IServiceCollection services)
        {
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            return services;
        }
    }
}
